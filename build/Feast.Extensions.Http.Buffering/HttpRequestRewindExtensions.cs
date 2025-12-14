using Feast.Extensions.Http.Buffering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Feast.Extensions.Http;

public static class HttpRequestRewindExtensions
{
    /// <param name="request">The <see cref="T:Microsoft.AspNetCore.Http.HttpRequest" /> to prepare.</param>
    extension(HttpRequest request)
    {
        /// <summary>
        /// Ensure the <paramref name="request" /> <see cref="P:Microsoft.AspNetCore.Http.HttpRequest.Body" /> can be read multiple times. Normally
        /// buffers request bodies in memory; writes requests larger than 30K bytes to disk.
        /// </summary>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </remarks>
        public IDisposable? EnableSwitchableBuffering() => request.EnableRewind();
        
        /// <summary>
        /// Ensure the <paramref name="request" /> <see cref="P:Microsoft.AspNetCore.Http.HttpRequest.Body" /> can be read multiple times. Normally
        /// buffers request bodies in memory; writes requests larger than <paramref name="bufferThreshold" /> bytes to
        /// disk.
        /// </summary>
        /// <param name="bufferThreshold">
        /// The maximum size in bytes of the in-memory <see cref="T:System.Buffers.ArrayPool`1" /> used to buffer the
        /// stream. Larger request bodies are written to disk.
        /// </param>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </remarks>
        public IDisposable? EnableSwitchableBuffering(int bufferThreshold) => request.EnableRewind(bufferThreshold);
        
        /// <summary>
        /// Ensure the <paramref name="request" /> <see cref="P:Microsoft.AspNetCore.Http.HttpRequest.Body" /> can be read multiple times. Normally
        /// buffers request bodies in memory; writes requests larger than 30K bytes to disk.
        /// </summary>
        /// <param name="bufferLimit">
        /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an
        /// <see cref="T:System.IO.IOException" />.
        /// </param>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </remarks>
        public IDisposable? EnableSwitchableBuffering(long bufferLimit) => request.EnableRewind(bufferLimit: bufferLimit);

        /// <summary>
        /// Ensure the <paramref name="request" /> <see cref="P:Microsoft.AspNetCore.Http.HttpRequest.Body" /> can be read multiple times. Normally
        /// buffers request bodies in memory; writes requests larger than <paramref name="bufferThreshold" /> bytes to
        /// disk.
        /// </summary>
        /// <param name="bufferThreshold">
        /// The maximum size in bytes of the in-memory <see cref="T:System.Buffers.ArrayPool`1" /> used to buffer the
        /// stream. Larger request bodies are written to disk.
        /// </param>
        /// <param name="bufferLimit">
        /// The maximum size in bytes of the request body. An attempt to read beyond this limit will cause an
        /// <see cref="T:System.IO.IOException" />.
        /// </param>
        /// <remarks>
        /// Temporary files for larger requests are written to the location named in the <c>ASPNETCORE_TEMP</c>
        /// environment variable, if any. If that environment variable is not defined, these files are written to the
        /// current user's temporary folder. Files are automatically deleted at the end of their associated requests.
        /// </remarks>
        public IDisposable? EnableSwitchableBuffering(int bufferThreshold, long bufferLimit) => 
            request.EnableRewind(bufferThreshold, bufferLimit);

        private IDisposable? EnableRewind(int bufferThreshold = DefaultBufferThreshold,
            long? bufferLimit = null)
        {
            var body = request.Body;
            if (body.CanSeek) return null;
            var stream = new SwitchableBufferingReadStream(body, new FileBufferingReadStream(body, bufferThreshold, bufferLimit, TempDirectoryFactory));
            request.Body = stream;
            request.HttpContext.Response.RegisterForDispose(stream);
            return new SwitchableBufferingReadStream.BufferSwitch(stream);
        }
    }

    internal const int DefaultBufferThreshold = 30720;

    internal static string TempDirectory
    {
        get
        {
            if (field != null) return field;
            var str = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();
            field = Directory.Exists(str) ? str : throw new DirectoryNotFoundException(str);
            return field;
        }
    }

    internal static Func<string> TempDirectoryFactory => (Func<string>) (() => TempDirectory);

    public static IDisposable? EnableRewind(
        this MultipartSection section,
        Action<IDisposable> registerForDispose,
        int bufferThreshold = DefaultBufferThreshold,
        long? bufferLimit = null)
    {
        var body = section.Body;
        if (body.CanSeek) return null;
        var stream = new SwitchableBufferingReadStream(body, new FileBufferingReadStream(body, bufferThreshold, bufferLimit, TempDirectoryFactory));
        section.Body = stream;
        registerForDispose(stream);
        return new SwitchableBufferingReadStream.BufferSwitch(stream);
    }
}