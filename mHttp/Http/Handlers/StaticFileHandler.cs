using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Web;

using m.Http.Extensions;

namespace m.Http.Handlers
{
    public class StaticFileHandler
    {
        class CachedFile
        {
            public FileResponse Response { get; }
            public HttpResponse GZippedResponse { get; }

            public DateTime LastModified { get; }
            public string ContentType { get; }

            public CachedFile(FileResponse response, HttpResponse gzippedResponse)
            {
                Response = response;
                LastModified = response.LastModified;
                ContentType = response.ContentType;
                GZippedResponse = gzippedResponse;
            }
        }

        static readonly HttpResponse NotFound = new HttpResponse(HttpStatusCode.NotFound);

        readonly int pathFilenameStartIndex;
        readonly string directory;
        readonly Func<byte[], byte[]> gzipFunc;

        readonly ConcurrentDictionary<string, CachedFile> cache;

        public StaticFileHandler(int pathFilenameStartIndex, DirectoryInfo dirInfo, Func<byte[], byte[]> gzipFuncImpl)
        {
            this.pathFilenameStartIndex = pathFilenameStartIndex; // position (string index) of incoming `request.Path` at which the requested filename is expected to begin
            directory = dirInfo.FullName;
            gzipFunc = gzipFuncImpl;

            cache = new ConcurrentDictionary<string, CachedFile>(StringComparer.Ordinal);
        }

        [Obsolete] public StaticFileHandler(string route, string directory, Func<byte[], byte[]> gzipFunc)
        {
            while (directory[0] == Path.DirectorySeparatorChar)
            {
                directory = directory.Substring(1);
            }

            pathFilenameStartIndex = route.Length - 1; // trailing wildcard *

            var dirInfo = new DirectoryInfo(directory);
            if (dirInfo.Exists)
            {
                this.directory = dirInfo.FullName;
            }
            else
            {
                throw new DirectoryNotFoundException($"The specified directory {dirInfo.FullName} could not be found.");
            }

            this.gzipFunc = gzipFunc;

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
                    return req.IsAcceptGZip() && cachedFile.GZippedResponse != null ? cachedFile.GZippedResponse : cachedFile.Response;
                }
            }
            else
            {
                var fileResponse = new FileResponse(fileInfo);

                cachedFile = new CachedFile(fileResponse, gzipFunc != null ? fileResponse.GZip(gzipFunc) : null);
                cache[fullName] = cachedFile;

                return req.IsAcceptGZip() && cachedFile.GZippedResponse != null ? cachedFile.GZippedResponse : cachedFile.Response;
            }
        }

        string GetFileFullName(IHttpRequest req)
        {
            var filename = HttpUtility.UrlDecode(req.Path.Substring(pathFilenameStartIndex));
            var fullPath = Path.Combine(directory, filename);

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
            return cache.TryGetValue(filename, out cached) && cached.LastModified <= ifModifiedSince;
        }

        static HttpResponse NotChanged(string contentType) => new HttpResponse(HttpStatusCode.NotModified, contentType); //TODO: cache
    }
}
