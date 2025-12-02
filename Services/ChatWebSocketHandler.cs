using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class ChatWebSocketHandler
{
    private static readonly ConcurrentDictionary<int, List<WebSocket>> _groupConnections = new();
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _groupLocks = new();

    public async Task HandleChatWebSocket(HttpContext context, int groupId)
    {
        Console.WriteLine($"🔗 WebSocket - New connection request for group {groupId}");

        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine($"✅ WebSocket - Accepted connection for group {groupId}");

            // Agregar conexión al grupo
            var connections = _groupConnections.GetOrAdd(groupId, _ => new List<WebSocket>());
            var @lock = _groupLocks.GetOrAdd(groupId, _ => new SemaphoreSlim(1, 1));

            await @lock.WaitAsync();
            try
            {
                connections.Add(webSocket);
                Console.WriteLine($"👥 WebSocket - Connections in group {groupId}: {connections.Count}");
            }
            finally
            {
                @lock.Release();
            }

            await HandleWebSocketMessages(webSocket, groupId);
        }
        else
        {
            Console.WriteLine($"❌ WebSocket - Not a WebSocket request for group {groupId}");
            context.Response.StatusCode = 400;
        }
    }

    private async Task HandleWebSocketMessages(WebSocket webSocket, int groupId)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            // Enviar mensaje de bienvenida
            var welcomeMessage = JsonSerializer.Serialize(new
            {
                type = "connection_established",
                message = "Connected to chat",
                groupId,
                timestamp = DateTime.UtcNow
            });

            await SendMessageAsync(webSocket, welcomeMessage);
            Console.WriteLine($"✅ WebSocket - Welcome message sent to group {groupId}");

            // Mantener la conexión activa
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"📨 WebSocket - Message received in group {groupId}: {message}");
                    
                    // Reenviar el mensaje a todos los clientes en el grupo
                    await BroadcastToGroup(groupId, message, webSocket);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"🔌 WebSocket - Close received for group {groupId}");
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"❌ WebSocket - WebSocketException in group {groupId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ WebSocket - General exception in group {groupId}: {ex.Message}");
        }
        finally
        {
            // Remover conexión del grupo
            if (_groupConnections.TryGetValue(groupId, out var connections))
            {
                var @lock = _groupLocks.GetOrAdd(groupId, _ => new SemaphoreSlim(1, 1));
                
                await @lock.WaitAsync();
                try
                {
                    connections.Remove(webSocket);
                    Console.WriteLine($"➖ WebSocket - Removed connection from group {groupId}. Remaining: {connections.Count}");

                    if (connections.Count == 0)
                    {
                        _groupConnections.TryRemove(groupId, out _);
                        _groupLocks.TryRemove(groupId, out _);
                        Console.WriteLine($"🗑️ WebSocket - Group {groupId} removed (no connections)");
                    }
                }
                finally
                {
                    @lock.Release();
                }
            }

            if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None);
                }
                catch { }
            }

            Console.WriteLine($"🔌 WebSocket - Connection fully closed for group {groupId}");
        }
    }

    private async Task SendMessageAsync(WebSocket webSocket, string message)
    {
        if (webSocket.State != WebSocketState.Open)
            return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ WebSocket - Error sending message: {ex.Message}");
        }
    }

    private async Task BroadcastToGroup(int groupId, string message, WebSocket sender)
    {
        if (!_groupConnections.TryGetValue(groupId, out var connections))
        {
            Console.WriteLine($"📢 WebSocket - No connections in group {groupId}");
            return;
        }

        var @lock = _groupLocks.GetOrAdd(groupId, _ => new SemaphoreSlim(1, 1));
        List<WebSocket> connectionsCopy;
        
        await @lock.WaitAsync();
        try
        {
            connectionsCopy = connections.ToList();
        }
        finally
        {
            @lock.Release();
        }

        Console.WriteLine($"📢 WebSocket - Broadcasting to {connectionsCopy.Count} connections in group {groupId}");

        var tasks = connectionsCopy
            .Where(conn => conn != sender && conn.State == WebSocketState.Open)
            .Select(conn => SendMessageAsync(conn, message));

        await Task.WhenAll(tasks);
        Console.WriteLine($"✅ WebSocket - Broadcast completed to group {groupId}");
    }

    public static async Task NotifyNewMessage(int groupId, object messageData)
    {
        Console.WriteLine($"📢 WebSocket - Notifying group {groupId} about new message");
        
        if (!_groupConnections.TryGetValue(groupId, out var connections))
        {
            Console.WriteLine($"📢 WebSocket - No connections to notify for group {groupId}");
            return;
        }

        var message = JsonSerializer.Serialize(new
        {
            type = "new_message",
            data = messageData,
            timestamp = DateTime.UtcNow
        });

        var @lock = _groupLocks.GetOrAdd(groupId, _ => new SemaphoreSlim(1, 1));
        List<WebSocket> connectionsCopy;
        
        await @lock.WaitAsync();
        try
        {
            connectionsCopy = connections.ToList();
        }
        finally
        {
            @lock.Release();
        }

        Console.WriteLine($"📢 WebSocket - Notifying {connectionsCopy.Count} connections in group {groupId}");

        var tasks = connectionsCopy
            .Where(conn => conn.State == WebSocketState.Open)
            .Select(conn => SendMessageAsyncStatic(conn, message));

        await Task.WhenAll(tasks);
        Console.WriteLine($"✅ WebSocket - Notification sent to group {groupId}");
    }

    private static async Task SendMessageAsyncStatic(WebSocket webSocket, string message)
    {
        if (webSocket.State != WebSocketState.Open)
            return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ WebSocket - Error in static send: {ex.Message}");
        }
    }
}