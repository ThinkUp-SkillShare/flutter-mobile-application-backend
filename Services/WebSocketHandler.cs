using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SkillShareBackend.Services
{
    public class WebSocketHandler
    {
        private static readonly Dictionary<string, List<WebSocket>> _sessions = new();
        private static readonly Dictionary<string, string> _userSessions = new();

        public async Task HandleWebSocket(HttpContext context, int groupId)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                await HandleConnection(webSocket, groupId.ToString(), userId);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private async Task HandleConnection(WebSocket webSocket, string sessionId, string userId)
        {
            // Registrar conexión en la sesión
            if (!_sessions.ContainsKey(sessionId))
            {
                _sessions[sessionId] = new List<WebSocket>();
            }
            _sessions[sessionId].Add(webSocket);
            _userSessions[userId] = sessionId;

            Console.WriteLine($"User {userId} joined session {sessionId}. Total connections: {_sessions[sessionId].Count}");

            try
            {
                var buffer = new byte[1024 * 4];
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received message from user {userId}: {message}");
                        
                        // Reenviar mensaje a todos los demás en la misma sesión
                        await BroadcastToOthers(sessionId, webSocket, message, userId);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            finally
            {
                _sessions[sessionId].Remove(webSocket);
                _userSessions.Remove(userId);
                
                if (_sessions[sessionId].Count == 0)
                {
                    _sessions.Remove(sessionId);
                }

                Console.WriteLine($"User {userId} left session {sessionId}. Remaining connections: {(_sessions.ContainsKey(sessionId) ? _sessions[sessionId].Count : 0)}");

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                    "Connection closed", CancellationToken.None);
            }
        }

        private async Task BroadcastToOthers(string sessionId, WebSocket sender, string message, string senderUserId)
        {
            if (_sessions.ContainsKey(sessionId))
            {
                var tasks = _sessions[sessionId]
                    .Where(ws => ws != sender && ws.State == WebSocketState.Open)
                    .Select(ws => 
                    {
                        try
                        {
                            // Agregar información del remitente al mensaje
                            var messageObj = JsonSerializer.Deserialize<JsonElement>(message);
                            var messageWithSender = new Dictionary<string, object>
                            {
                                ["type"] = messageObj.GetProperty("type").GetString(),
                                ["data"] = messageObj,
                                ["senderId"] = senderUserId,
                                ["timestamp"] = DateTime.UtcNow
                            };

                            var jsonMessage = JsonSerializer.Serialize(messageWithSender);
                            var buffer = Encoding.UTF8.GetBytes(jsonMessage);

                            return ws.SendAsync(
                                new ArraySegment<byte>(buffer),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending message: {ex.Message}");
                            return Task.CompletedTask;
                        }
                    });

                await Task.WhenAll(tasks);
            }
        }

        public static int GetConnectionCount(string sessionId)
        {
            return _sessions.ContainsKey(sessionId) ? _sessions[sessionId].Count : 0;
        }

        public static List<string> GetActiveSessions()
        {
            return _sessions.Keys.ToList();
        }

        public static int GetTotalConnections()
        {
            return _sessions.Values.Sum(session => session.Count);
        }
    }
}