using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using m.Http.Backend.Tcp;

namespace m.Http
{
    public class HttpsBackend : HttpBackend
    {
        readonly X509Certificate2 serverCertificate;
        readonly SslProtocols sslProtocols;

        public HttpsBackend(X509Certificate2 serverCertificate,
                            SslProtocols sslProtocols,
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
            this.sslProtocols = sslProtocols;
        }

        internal override async Task<HttpSession> CreateSession(long sessionId, TcpClient client)
        {
            var sslStream = new SslStream(client.GetStream());
            await sslStream.AuthenticateAsServerAsync(serverCertificate, false, sslProtocols, false).ConfigureAwait(false);

            return new HttpSession(sessionId, client, sslStream, true, maxKeepAlives, sessionReadBufferSize, (int)sessionReadTimeout.TotalMilliseconds, (int)sessionWriteTimeout.TotalMilliseconds);
        }
    }
}
