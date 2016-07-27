using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Web;

using m.Http.Extensions;

namespace m.Http.Handlers
{
    public class StaticFileHandler // not threadsafe really, but good for now
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
        static readonly HttpResponse Forbidden = new HttpResponse(HttpStatusCode.Forbidden);

        readonly int reqPathFilenameStartIndex;
        readonly string directory;
        readonly Func<byte[], byte[]> gzipFunc;

        readonly ConcurrentDictionary<string, CachedFile> cache;

        public StaticFileHandler(int reqPathFilenameStartIndex, DirectoryInfo dirInfo, Func<byte[], byte[]> gzipFuncImpl)
        {
            this.reqPathFilenameStartIndex = reqPathFilenameStartIndex; // position (string index) of incoming `request.Path` at which the requested filename is expected to begin
            directory = dirInfo.FullName;
            gzipFunc = gzipFuncImpl;

            cache = new ConcurrentDictionary<string, CachedFile>(StringComparer.Ordinal);
        }

        public HttpResponse Handle(IHttpRequest req)
        {
            var requestedFilename = HttpUtility.UrlDecode(req.Path.Substring(reqPathFilenameStartIndex));
            var absFilename = Path.GetFullPath(Path.Combine(directory, requestedFilename));
            if (!absFilename.StartsWith(directory))
            {
                return Forbidden;
            }

            var fileInfo = new FileInfo(absFilename);
            if (!fileInfo.Exists) // cached but deleted
            {
                return NotFound;
            }

            DateTime ifModifiedSince;
            CachedFile cachedFile;

            var checkLastModified = req.TryGetIfLastModifiedSince(out ifModifiedSince); // remote client has a cached version
            var hasCachedResponse = checkLastModified ? TryGetCachedFileResponse(absFilename, ifModifiedSince, out cachedFile) : TryGetCachedFileResponse(absFilename, out cachedFile);

            if (hasCachedResponse && fileInfo.LastWriteTimeUtc <= cachedFile.LastModified)
            {
                if (checkLastModified)
                {
                    return NotChanged(cachedFile.ContentType);
                }
            }
            else // Load fresh from disk
            {
                var fileResponse = new FileResponse(fileInfo);
                cachedFile = new CachedFile(fileResponse, gzipFunc != null ? fileResponse.GZip(gzipFunc) : null);
                cache[absFilename] = cachedFile;
            }

            return req.IsAcceptGZip() && cachedFile.GZippedResponse != null ? cachedFile.GZippedResponse : cachedFile.Response;
        }

        bool TryGetCachedFileResponse(string fullName, out CachedFile cached) => cache.TryGetValue(fullName, out cached);

        bool TryGetCachedFileResponse(string filename, DateTime ifModifiedSince, out CachedFile cached) => cache.TryGetValue(filename, out cached) && cached.LastModified <= ifModifiedSince;

        static HttpResponse NotChanged(string contentType) => new HttpResponse(HttpStatusCode.NotModified, contentType); //TODO: cache
    }
}
