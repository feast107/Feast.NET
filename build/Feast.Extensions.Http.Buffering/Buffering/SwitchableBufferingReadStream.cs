namespace Feast.Extensions.Http.Buffering;

/// <summary>
/// Normal reading
/// </summary>
/// <param name="original"></param>
/// <param name="bufferingStream"></param>
public class SwitchableBufferingReadStream(Stream original, Stream bufferingStream) : Stream
{
    internal sealed class BufferSwitch(SwitchableBufferingReadStream stream) : IDisposable
    {
        public void Dispose() => stream.StopBuffering();
    }

    private long buffered;
    private bool buffering = true;

    public override void Flush()                                     => throw new NotImplementedException();
    public override int  Read(byte[] buffer, int offset, int count)  => throw new NotImplementedException();
    public override void SetLength(long value)                       => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override long Seek(long offset, SeekOrigin origin) => origin switch
    {
        SeekOrigin.Begin   => Position = offset,
        SeekOrigin.Current => Position += offset,
        _                  => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
    };

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
    {
        var start = Position;
        if (buffering)
        {
            if (start < buffered)
            {
                bufferingStream.Seek(start, SeekOrigin.Begin);
                var read = await bufferingStream.ReadAsync(buffer, cancellationToken);
                Position = start + read;
                buffered = Math.Max(buffered, Position);
                return read;
            }
            if (start == buffered)
            {
                var read            = await bufferingStream.ReadAsync(buffer, cancellationToken);
                Position = buffered += read;
                return read;
            }
            else
            {
                var ignore = start - buffered;
                bufferingStream.Seek(buffered, SeekOrigin.Begin);
                var actual          = await bufferingStream.ReadAsync(new byte[ignore], 0, (int)ignore, cancellationToken);
                var read            = await bufferingStream.ReadAsync(buffer, cancellationToken);
                Position = buffered += actual + read;
                return read;
            }
        }
        if (start >= buffered)
        {
            var read = await original.ReadAsync(buffer, cancellationToken);
            Position = start + read;
            return read;
        }
        bufferingStream.Seek(start, SeekOrigin.Begin);
        var fromBuffer     = Math.Min(buffered - start, buffer.Length);
        var readFromBuffer = fromBuffer <= 0 ? 0 : await bufferingStream.ReadAsync(buffer[..(int)fromBuffer], cancellationToken);
        var rest           = buffer.Length - readFromBuffer;
        var all            = readFromBuffer + (rest <= 0 ? 0 : await original.ReadAsync(buffer.Slice(readFromBuffer, rest), cancellationToken));
        Position += all;
        return all;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);

    public override bool CanRead  => true;
    public override bool CanSeek  => true;
    public override bool CanWrite => false;
    public override long Length   => throw new NotImplementedException();
    public override long Position { get; set; }

    public void StopBuffering() => buffering = false;

    public override ValueTask DisposeAsync() => bufferingStream.DisposeAsync();
}