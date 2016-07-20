using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;

using m.Http.Extensions;

namespace m.Http.Handlers
{
    public class StaticFileHandler
    {
        class CachedFile
        {
            public FileResponse Response { get; }
            public HttpResponse GZippedResponse { get; }

            public DateTime LastModified { get { return Response.LastModified; } }
            public string ContentType { get { return Response.ContentType; } }

            public CachedFile(FileInfo fileInfo)
            {
                Response = new FileResponse(fileInfo);
                GZippedResponse = this.Response.GZip();
            }
        }

        static readonly HttpResponse NotFound = new HttpResponse(HttpStatusCode.NotFound);

        readonly string route;
        readonly string directory;

        readonly ConcurrentDictionary<string, CachedFile> cache;

        public StaticFileHandler(string route, string directory)
        {
            while (directory[0] == Path.DirectorySeparatorChar)
            {
                directory = directory.Substring(1);
            };

            this.route = route;

            var dirInfo = new DirectoryInfo(directory);
            if (dirInfo.Exists)
            {
                this.directory = dirInfo.FullName;
            }
            else
            {
                throw new DirectoryNotFoundException($"The specified directory ${directory} could not be found.");
            }

            cache = new ConcurrentDictionary<string, CachedFile>(StringComparer.Ordinal);
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
            CachedFile cachedFile;

            var checkLastModified = req.TryGetIfLastModifiedSince(out ifModifiedSince); // remote client has a cached version
            var hasCachedResponse = checkLastModified ? TryGetCachedFileResponse(fullName, ifModifiedSince, out cachedFile) : TryGetCachedFileResponse(fullName, out cachedFile);

            if (hasCachedResponse && fileInfo.LastWriteTimeUtc <= cachedFile.LastModified)
            {
                if (checkLastModified)
                {
                    return NotChanged(cachedFile.ContentType);
                }
                else
                {
                    return req.IsAcceptGZip() ? cachedFile.GZippedResponse : cachedFile.Response;
                }
            }
            else
            {
                cachedFile = new CachedFile(fileInfo);
                cache[fullName] = cachedFile;

                return req.IsAcceptGZip() ? cachedFile.GZippedResponse : cachedFile.Response;
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

        bool TryGetCachedFileResponse(string fullName, out CachedFile cached) => cache.TryGetValue(fullName, out cached);

        bool TryGetCachedFileResponse(string filename, DateTime ifModifiedSince, out CachedFile cached)
        {
            if (cache.TryGetValue(filename, out cached) && cached.LastModified <= ifModifiedSince)
            {
                return true;
            }
            else
            {
                cached = null;
                return false;
            }
        }

        static HttpResponse NotChanged(string contentType) => new HttpResponse(HttpStatusCode.NotModified, contentType); //TODO: cache
    }
}
