using System;
using System.Threading.Tasks;

namespace m.Http
{
    public interface IWebSocketSession : IDisposable
    {
        long Id { get; }

        bool IsOpen { get; }

        Task<WebSocketMessage> ReadNextMessageAsync();

        void SendText(string text);
        void SendBinary(byte[] blob);
        void SendClose(ushort statusCode=0, string reason=null);
        void SendPing();
        void SendPong();

        void CloseSession(ushort statusCode=0, string reason=null);
    }
}
