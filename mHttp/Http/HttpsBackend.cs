using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using m.Http.Backend.Tcp;

namespace m.Http
{
    public class HttpsBackend : HttpBackend
    {
        readonly X509Certificate2 serverCertificate;

        public HttpsBackend(X509Certificate2 serverCertificate,
                            IPAddress address,
                            int port,
                            int maxKeepAlives=100,
                            int backlog=128,
                            int sessionReadBufferSize=1024,
                            int sessionReadTimeoutMs=5000,
                            int sessionWriteTimeoutMs=5000) : base(address,
                                                                   port,
                                                                   maxKeepAlives,
                                                                   backlog,
                                                                   sessionReadBufferSize,
                                                                   sessionReadTimeoutMs,
                                                                   sessionWriteTimeoutMs)
        {
            this.serverCertificate = serverCertificate;
        }

        internal override async Task<HttpSession> CreateSession(long sessionId,
                                                                TcpClient client,
                                                                int _maxKeepAlives,
                                                                int _sessionReadBufferSize,
                                                                TimeSpan _sessionReadTimeout,
                                                                TimeSpan _sessionWriteTimeout)
        {
            var sslStream = new SslStream(client.GetStream());
            await sslStream.AuthenticateAsServerAsync(serverCertificate, false, System.Security.Authentication.SslProtocols.Tls12, false);

            return new HttpSession(sessionId, client, sslStream, true, _maxKeepAlives, _sessionReadBufferSize, _sessionReadTimeout, _sessionWriteTimeout);
        }
    }
}
