using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KickChat
{
    public sealed class KickMessage
    {
        internal KickMessage(string username, string content, string messageType, string[] badgeTypes)
        {
            Username = username;
            Content = content;
            MessageType = messageType;
            BadgeTypes = badgeTypes;
        }

        public string Username { get; private set; }
        public string Content { get; private set; }
        public string MessageType { get; private set; }
        public string[] BadgeTypes { get; private set; }
    }

    public sealed class KickChatClient
    {
        private readonly long _chatroomId;
        private readonly string _appKey;
        private readonly string _cluster;
        private readonly Action<string> _log;
        private readonly object _sync = new object();
        private CancellationTokenSource _cancellation;
        private Task _worker;
        private ClientWebSocket _socket;

        public KickChatClient(long chatroomId, string appKey, string cluster, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(appKey))
                throw new ArgumentException("An app key is required.", nameof(appKey));
            if (string.IsNullOrWhiteSpace(cluster))
                throw new ArgumentException("A cluster is required.", nameof(cluster));

            _chatroomId = chatroomId;
            _appKey = appKey;
            _cluster = cluster;
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public event Action<KickMessage> OnMessage;

        // Start the reconnecting client on a worker so callers are not blocked.
        public void Start()
        {
            lock (_sync)
            {
                if (_worker != null && !_worker.IsCompleted)
                    return;

                _cancellation = new CancellationTokenSource();
                var token = _cancellation.Token;
                _worker = Task.Run(() => RunAsync(token));
            }
        }

        // Cancel the receive loop and send a WebSocket close frame when possible.
        public void Stop()
        {
            ClientWebSocket socket;
            lock (_sync)
            {
                if (_cancellation != null)
                    _cancellation.Cancel();
                socket = _socket;
            }

            if (socket != null &&
                (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived))
            {
                try
                {
                    socket.CloseOutputAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client stopping",
                        CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    Log("Could not send the WebSocket close frame: " + exception.Message);
                }
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // Increase the delay after each failure, capping it to avoid an excessively long wait.
            var retryDelaySeconds = 2;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ClientWebSocket socket = null;
                    try
                    {
                        socket = new ClientWebSocket();
                        lock (_sync)
                            _socket = socket;

                        var endpoint = "wss://ws-" + _cluster + ".pusher.com/app/" + _appKey +
                            "?protocol=7&client=js&version=8.4.0&flash=false";
                        await socket.ConnectAsync(new Uri(endpoint), cancellationToken).ConfigureAwait(false);

                        if (await ReadFramesAsync(socket, cancellationToken).ConfigureAwait(false))
                            return;

                        if (!cancellationToken.IsCancellationRequested)
                            Log("Kick chat disconnected; reconnecting.");
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        Log("Kick chat connection failed: " + exception.Message);
                    }
                    finally
                    {
                        lock (_sync)
                        {
                            if (ReferenceEquals(_socket, socket))
                                _socket = null;
                        }
                        if (socket != null)
                        {
                            try
                            {
                                socket.Dispose();
                            }
                            catch (Exception exception)
                            {
                                Log("Could not dispose the WebSocket: " + exception.Message);
                            }
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    retryDelaySeconds = Math.Min(retryDelaySeconds * 2, 60);
                }
            }
            catch (Exception exception)
            {
                Log("Kick chat background loop failed: " + exception.Message);
            }
        }

        private async Task<bool> ReadFramesAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            using (var frame = new MemoryStream())
            {
                // A WebSocket message can span receives; process it only at EndOfMessage.
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                        return false;

                    frame.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage)
                        continue;

                    var raw = Encoding.UTF8.GetString(frame.GetBuffer(), 0, (int)frame.Length);
                    frame.SetLength(0);
                    if (result.MessageType == WebSocketMessageType.Text &&
                        await HandleFrameAsync(socket, raw, cancellationToken).ConfigureAwait(false))
                        return true;
                }
            }
            return false;
        }

        private async Task<bool> HandleFrameAsync(
            ClientWebSocket socket,
            string raw,
            CancellationToken cancellationToken)
        {
            var root = JToken.Parse(raw);
            var eventName = GetStringProperty(root, "event");
            if (eventName == "pusher:connection_established")
            {
                var subscribe = JsonConvert.SerializeObject(new
                {
                    @event = "pusher:subscribe",
                    data = new
                    {
                        auth = "",
                        channel = "chatrooms." + _chatroomId + ".v2"
                    }
                });
                await SendAsync(socket, subscribe, cancellationToken).ConfigureAwait(false);
            }
            else if (eventName == "pusher:ping")
            {
                await SendAsync(socket, "{\"event\":\"pusher:pong\",\"data\":{}}", cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (eventName == "pusher:error")
            {
                var errorData = ParseDataObject(root);
                var code = GetProperty(errorData, "code");
                var rejectedKey = GetStringProperty(errorData, "code") == "4001" ||
                    (code != null && code.Type == JTokenType.Integer && code.ToString() == "4001");
                if (rejectedKey)
                {
                    Log("Kick rejected the app key (it was probably rotated). Update AppKey in the config.");
                    return true;
                }
                throw new InvalidOperationException("Pusher reported a connection error.");
            }
            else if (eventName == "App\\Events\\ChatMessageEvent")
            {
                // Kick nests this event's JSON in the outer frame's data string.
                var data = ParseDataObject(root);
                var sender = GetProperty(data, "sender");
                var identity = GetProperty(sender, "identity");
                var badges = GetProperty(identity, "badges");
                var badgeTypes = new List<string>();
                if (badges is JArray badgeArray)
                {
                    foreach (var badge in badgeArray)
                    {
                        var badgeType = GetStringProperty(badge, "type");
                        if (badgeType != null)
                            badgeTypes.Add(badgeType);
                    }
                }

                var chatMessage = new KickMessage(
                    GetStringProperty(sender, "username") ?? "",
                    GetStringProperty(data, "content") ?? "",
                    GetStringProperty(data, "type") ?? "",
                    badgeTypes.ToArray());
                var handler = OnMessage;
                if (handler != null)
                {
                    try
                    {
                        handler(chatMessage);
                    }
                    catch (Exception exception)
                    {
                        Log("Kick chat message handler failed: " + exception.Message);
                    }
                }
            }
            return false;
        }

        private static JToken ParseDataObject(JToken root)
        {
            var data = GetProperty(root, "data");
            if (data != null && data.Type == JTokenType.String)
            {
                var json = data.Value<string>();
                if (json == null)
                    return null;
                return JToken.Parse(json);
            }
            return data;
        }

        private static JToken GetProperty(JToken element, string name)
        {
            return element is JObject obj ? obj[name] : null;
        }

        private static string GetStringProperty(JToken element, string name)
        {
            var value = GetProperty(element, name);
            return value != null && value.Type == JTokenType.String ? value.Value<string>() : null;
        }

        private static Task SendAsync(
            ClientWebSocket socket,
            string message,
            CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            return socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }

        private void Log(string message)
        {
            try
            {
                _log(message);
            }
            catch
            {
                // A logging callback must not terminate the background loop.
            }
        }
    }
}
