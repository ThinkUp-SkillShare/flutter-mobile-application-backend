using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection; // Necesario para ScopeFactory
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.Models;

namespace SkillShareBackend.Services;

public class WebSocketHandler
{
    // Necesitamos ScopeFactory porque WebSocketHandler es Singleton y DbContext es Scoped
    private readonly IServiceScopeFactory _scopeFactory;
    
    private static readonly ConcurrentDictionary<string, List<WebSocketConnection>> _callSessions = new();
    private static readonly ConcurrentDictionary<string, string> _userCallSessions = new();

    public WebSocketHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleCallWebSocket(HttpContext context, string callId)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var userId = context.Request.Query["userId"].ToString();
        
        if (string.IsNullOrEmpty(userId))
            userId = context.User?.Identity?.Name;

        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("❌ No user ID provided for WebSocket connection");
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "User ID required", CancellationToken.None);
            return;
        }

        await HandleCallConnection(webSocket, callId, userId);
    }

    private async Task HandleCallConnection(WebSocket webSocket, string callId, string userId)
    {
        var connection = new WebSocketConnection
        {
            WebSocket = webSocket,
            UserId = userId,
            CallId = callId,
            ConnectedAt = DateTime.UtcNow
        };

        if (!_callSessions.ContainsKey(callId)) _callSessions[callId] = new List<WebSocketConnection>();
        _callSessions[callId].Add(connection);
        _userCallSessions[userId] = callId;

        // Actualizar BD al conectarse
        await UpdateCallParticipantCount(callId, _callSessions[callId].Count);

        Console.WriteLine($"✅ User {userId} connected to call {callId}. Total connections: {_callSessions[callId].Count}");

        // Notificar a OTROS usuarios (user-joined)
        // Importante: El cliente receptor debe usar este ID para iniciar la oferta WebRTC
        await BroadcastToCallOthers(callId, connection, new
        {
            type = "user-joined",
            data = new { userId },
            callId,
            senderId = userId,
            timestamp = DateTime.UtcNow.Ticks
        });

        try
        {
            var buffer = new byte[1024 * 16]; 
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessCallMessage(callId, connection, message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleUserDisconnection(connection);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error/Disconnect for user {userId}: {ex.Message}");
            await HandleUserDisconnection(connection);
        }
    }

    private async Task ProcessCallMessage(string callId, WebSocketConnection sender, string message)
    {
        try
        {
            var messageData = JsonSerializer.Deserialize<JsonElement>(message);
            if (!messageData.TryGetProperty("type", out var typeProperty)) return;

            var messageType = typeProperty.GetString();
            
            // Lógica para routing de mensajes WebRTC (P2P Signaling)
            // Si el mensaje tiene un 'targetUserId', deberíamos enviarlo solo a ese usuario.
            // Para simplificar y mantener compatibilidad con Mesh actual, hacemos broadcast,
            // pero el cliente debe filtrar si el mensaje es para él.
            
            JsonElement payload;
            if (messageData.TryGetProperty("data", out var dataProp)) payload = dataProp;
            else if (messageType == "offer") payload = messageData.GetProperty("offer");
            else if (messageType == "answer") payload = messageData.GetProperty("answer");
            else if (messageType == "ice-candidate") payload = messageData.GetProperty("candidate");
            else payload = messageData;

            // Extraer targetUserId si existe (para dirigir la señalización)
            string targetUserId = null;
            if (messageData.TryGetProperty("targetUserId", out var targetProp))
            {
                targetUserId = targetProp.GetString();
            }

            var enhancedMessage = new
            {
                type = messageType,
                data = payload,
                callId,
                senderId = sender.UserId,
                targetUserId = targetUserId, // Reenviar el target
                timestamp = DateTime.UtcNow.Ticks
            };

            // Switch simplificado
            switch (messageType)
            {
                case "offer":
                case "answer":
                case "ice-candidate":
                    // Señalización WebRTC siempre va a los demas
                    await BroadcastToCallOthers(callId, sender, enhancedMessage);
                    break;

                case "user-joined":
                case "user-left":
                    await BroadcastToCallAll(callId, enhancedMessage);
                    break;
                    
                default:
                    // Mensajes genéricos (chat, mute status, etc)
                    await BroadcastToCallOthers(callId, sender, enhancedMessage);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing message: {ex.Message}");
        }
    }

    private async Task HandleUserDisconnection(WebSocketConnection connection)
    {
        var callId = connection.CallId;
        var userId = connection.UserId;
        bool removed = false;

        if (_callSessions.ContainsKey(callId))
        {
            lock (_callSessions[callId]) 
            {
                removed = _callSessions[callId].Remove(connection);
            }
        }
        
        _userCallSessions.TryRemove(userId, out _);

        if (removed)
        {
             // Actualizar BD
            int remainingCount = _callSessions.ContainsKey(callId) ? _callSessions[callId].Count : 0;
            await UpdateCallParticipantCount(callId, remainingCount);
            
            Console.WriteLine($"👤 User {userId} disconnected. Remaining: {remainingCount}");

            if (remainingCount == 0)
            {
                _callSessions.TryRemove(callId, out _);
                Console.WriteLine($"🗑️ Call session {callId} removed (empty)");
                // Opcional: Marcar IsActive=false en DB si todos salen
                await UpdateCallStatus(callId, false); 
            }
            else
            {
                await BroadcastToCallAll(callId, new
                {
                    type = "user-left",
                    data = new { userId },
                    callId,
                    senderId = userId,
                    timestamp = DateTime.UtcNow.Ticks
                });
            }
        }

        try
        {
            if (connection.WebSocket.State == WebSocketState.Open || 
                connection.WebSocket.State == WebSocketState.CloseReceived)
            {
                await connection.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
        }
        catch { /* Ignorar error de cierre */ }
    }

    // Método helper para actualizar BD
    private async Task UpdateCallParticipantCount(string callId, int count)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var call = await dbContext.GroupCalls.FirstOrDefaultAsync(c => c.CallId == callId);
            if (call != null)
            {
                call.ParticipantCount = count;
                await dbContext.SaveChangesAsync();
            }
        }
    }

    private async Task UpdateCallStatus(string callId, bool isActive)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var call = await dbContext.GroupCalls.FirstOrDefaultAsync(c => c.CallId == callId);
            if (call != null)
            {
                call.IsActive = isActive;
                if (!isActive) call.EndedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }
    }

    // ... (Mantener BroadcastToCallOthers y BroadcastToCallAll igual, son genéricos)
    private async Task BroadcastToCallOthers(string callId, WebSocketConnection sender, object message)
    {
        if (!_callSessions.ContainsKey(callId)) return;
        var messageJson = JsonSerializer.Serialize(message);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);

        // Copiar lista para iterar seguro
        var connections = _callSessions[callId].Where(c => c.UserId != sender.UserId).ToList();

        foreach (var conn in connections)
        {
            if (conn.WebSocket.State == WebSocketState.Open)
                await conn.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private async Task BroadcastToCallAll(string callId, object message)
    {
        if (!_callSessions.ContainsKey(callId)) return;
        var messageJson = JsonSerializer.Serialize(message);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);

        var connections = _callSessions[callId].ToList();
        foreach (var conn in connections)
        {
             if (conn.WebSocket.State == WebSocketState.Open)
                await conn.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public static int GetCallParticipantCount(string callId)
    {
        return _callSessions.TryGetValue(callId, out var connections) ? connections.Count : 0;
    }

    public static void CleanupCall(string callId)
    {
        if (_callSessions.TryRemove(callId, out var connections))
        {
            foreach(var conn in connections) conn.WebSocket.Abort();
        }
    }
    
    public class WebSocketConnection
    {
        public WebSocket WebSocket { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
    }
}