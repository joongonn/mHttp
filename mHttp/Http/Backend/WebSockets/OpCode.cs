namespace m.Http.Backend.WebSockets
{
    public enum OpCode : byte
    {
        Continuation,
        Text,
        Binary,
        Close = 8,
        Ping = 9,
        Pong = 10
    }
}
