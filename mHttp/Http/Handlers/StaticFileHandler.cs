using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;

using m.Http.Extensions;

namespace m.Http.Handlers
{
    public class StaticFileHandler
    {
        static readonly HttpResponse NotFound = new HttpResponse(HttpStatusCode.NotFound);

        readonly string route;
        readonly string directory;

        readonly ConcurrentDictionary<string, FileResponse> cache;

        public StaticFileHandler(string route, string directory)
        {
            while (directory[0] == Path.DirectorySeparatorChar)
            {
                directory = directory.Substring(1);
            };

            this.route = route;
            this.directory = AppDomain.CurrentDomain.BaseDirectory + directory;

            cache = new ConcurrentDictionary<string, FileResponse>(StringComparer.Ordinal);
        }

        public HttpResponse Handle(IHttpRequest req)
        {
            var fullName = GetFileFullName(req);
            var fileInfo = new FileInfo(fullName);

            if (!fileInfo.Exists) // cached but deleted
            {
                return NotFound;
            }

            DateTime ifModifiedSince;
            FileResponse fileResponse;

            var checkLastModified = req.TryGetIfLastModifiedSince(out ifModifiedSince);
            var hasCachedResponse = checkLastModified ? TryGetCachedFileResponse(fullName, ifModifiedSince, out fileResponse) : TryGetCachedFileResponse(fullName, out fileResponse);

            if (hasCachedResponse && fileInfo.LastWriteTimeUtc <= fileResponse.LastModified)
            {
                if (checkLastModified)
                {
                    return NotChanged(fileResponse.ContentType);
                }
                else
                {
                    return fileResponse;
                }
            }
            else
            {
                fileResponse = new FileResponse(fileInfo);
                cache[fullName] = fileResponse;

                return fileResponse;
            }
        }

        string GetFileFullName(IHttpRequest req)
        {
            var filename = req.Path.Substring(route.Length - 1); // trailing wildcard *
            var fullPath = Path.GetFullPath(directory + filename);

            if (fullPath.StartsWith(directory))
            {
                return fullPath;
            }
            else
            {
                throw new RequestException("Illegal file access", HttpStatusCode.Forbidden);
            }
        }

        bool TryGetCachedFileResponse(string fullName, out FileResponse fileResponse)
        {
            return cache.TryGetValue(fullName, out fileResponse);
        }

        bool TryGetCachedFileResponse(string filename, DateTime ifModifiedSince, out FileResponse fileResponse)
        {
            if (cache.TryGetValue(filename, out fileResponse) && fileResponse.LastModified <= ifModifiedSince)
            {
                return true;
            }
            else
            {
                fileResponse = null;
                return false;
            }
        }

        static HttpResponse NotChanged(string contentType)
        {
            return new HttpResponse(HttpStatusCode.NotModified, contentType); //TODO: cache
        }
    }
}
