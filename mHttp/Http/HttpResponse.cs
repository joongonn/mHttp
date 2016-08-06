using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using m.Http.Backend.Tcp;

namespace m.Http
{
    public class TextResponse : HttpResponse
    {
        public TextResponse(string text) : base(HttpStatusCode.OK, ContentTypes.Plain, new HttpBody.ByteArray(Encoding.UTF8.GetBytes(text))) { }
    }

    public abstract class FileResponse : HttpResponse
    {
        public string Filename { get; }
        public DateTime LastModified { get; }

        FileResponse(FileInfo fileInfo, HttpBody body) : base(HttpStatusCode.OK, MimeMapping.GetMimeMapping(fileInfo.Name), body)
        {
            Filename = fileInfo.Name;
            LastModified = fileInfo.LastWriteTimeUtc;
            Headers[HttpHeader.LastModified] = LastModified.ToString("R");
        }

        public sealed class Buffered : FileResponse
        {
            public Buffered(FileInfo fileInfo) : base(fileInfo, new HttpBody.ByteArray(File.ReadAllBytes(fileInfo.FullName))) { }
        }

        public sealed class Stream : FileResponse
        {
            public Stream(FileInfo fileInfo) : base(fileInfo, new HttpBody.Streamable(fileInfo.OpenRead(), fileInfo.Length)) { }
        }
    }

    public class RedirectResponse : HttpResponse
    {
        public RedirectResponse(string location) : base(HttpStatusCode.Moved, ContentTypes.Html)
        {
            Headers["Location"] = location;
        }
    }

    //TODO: will need a GZippedResponse that wraps a GZippedStream over the output `toStream`

    //TODO: consider making this abstract
    //TODO: reconsider mutability of Headers
    public class HttpResponse
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string StatusDescription { get; private set; }
        public string ContentType { get; private set; }
        public IDictionary<string, string> Headers { get; private set; }

        public HttpBody Body { get; private set; }

        public HttpResponse(HttpStatusCode statusCode) : this(statusCode, ContentTypes.Html) { }

        public HttpResponse(HttpStatusCode statusCode, string contentType) : this(statusCode, contentType, HttpBody.Empty.Instance) { }

        public HttpResponse(HttpStatusCode statusCode, string contentType, HttpBody body) : this(statusCode, statusCode.ToString(), contentType, new Dictionary<string, string>(0, StringComparer.OrdinalIgnoreCase), body) { }

        public HttpResponse(HttpStatusCode statusCode, string statusDescription, string contentType) : this(statusCode, statusDescription, contentType, new Dictionary<string, string>(0, StringComparer.OrdinalIgnoreCase), HttpBody.Empty.Instance) { }

        public HttpResponse(HttpStatusCode statusCode,
                            string statusDescription,
                            string contentType,
                            IDictionary<string, string> headers, //TODO: copy or ref
                            HttpBody body)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            ContentType = contentType;
            Headers = headers;
            Body = body;
        }

        internal virtual async Task<int> WriteToAsync(Stream toStream, int keepAlives, TimeSpan keepAliveTimeout)
        {
            var contentLength = Body.Length; //TODO: unknown length
            var bytesWritten = 0;

            try
            {
                var statusAndHeaders = HttpResponseWriter.GetStatusAndHeaders((int)StatusCode,
                                                                              StatusDescription,
                                                                              ContentType,
                                                                              contentLength,
                                                                              keepAlives,
                                                                              keepAliveTimeout,
                                                                              Headers);
                
                await toStream.WriteAsync(statusAndHeaders, 0, statusAndHeaders.Length).ConfigureAwait(false);
                bytesWritten += statusAndHeaders.Length;

                if (contentLength > 0)
                {
                    var disposableBody = Body as IDisposable;
                    if (disposableBody == null)
                    {
                        bytesWritten += await Body.WriteToAsync(toStream).ConfigureAwait(false);
                    }
                    else
                    {
                        using (disposableBody)
                        {
                            bytesWritten += await Body.WriteToAsync(toStream).ConfigureAwait(false);
                        }
                    }
                }

                return bytesWritten;
            }
            catch (Exception e)
            {
                throw new SessionStreamException("Exception writing to stream", e);
            }
        }
    }

    //TODO: explore stronger response typing with generics
    // public class HttpResponse<T> : HttpResponse where T : HttpBody
    // {
    //     public HttpResponse(HttpStatusCode statusCode, string contentType, T body) : base(statusCode, contentType, body) { }
    //
    //     new public T Body { get { return (T)base.Body; } }
    // }

    //TODO: get rid of this
    sealed class ErrorResponse : HttpResponse
    {
        public readonly Exception Exception;

        public ErrorResponse(HttpStatusCode statusCode) : base(statusCode, ContentTypes.Html)
        {
            Exception = null;
        }

        public ErrorResponse(HttpStatusCode statusCode, string statusDescription) : base(statusCode, statusDescription, ContentTypes.Html)
        {
            Exception = null;
        }

        public ErrorResponse(HttpStatusCode statusCode, Exception exception) : base(statusCode, ContentTypes.Html)
        {
            Exception = exception;
        }
    }
}
