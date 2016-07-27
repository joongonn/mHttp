namespace m.Http
{
    public abstract class WebSocketMessage
    {
        public enum Type
        {
            Text,
            Binary,
            Close,
            Ping,
            Pong
        }

        public sealed class Text : WebSocketMessage
        {
            public string Payload { get; private set; }

            public Text(string payload) : base(Type.Text)
            {
                Payload = payload;
            }
        }

        public sealed class Binary : WebSocketMessage
        {
            public byte[] Payload { get; private set; }

            public Binary(byte[] payload) : base(Type.Binary)
            {
                Payload = payload;
            }
        }

        public sealed class Close : WebSocketMessage
        {
            public ushort StatusCode { get; private set; }
            public string Reason { get; private set; }

            public Close(ushort statusCode, string reason) : base(Type.Close)
            {
                StatusCode = statusCode;
                Reason = reason;
            }
        }

        public sealed class Ping : WebSocketMessage
        {
            public Ping() : base(Type.Ping) { }
        }

        public sealed class Pong : WebSocketMessage
        {
            public Pong() : base(Type.Pong) { }
        }

        public Type MessageType { get; private set; }

        WebSocketMessage(Type messageType)
        {
            MessageType = messageType;
        }
    }
}
