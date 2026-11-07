using System.Buffers;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

/// <summary>
/// The span and memory members of <see cref="Stream"/> and the cancellable read to the end of
/// a <see cref="TextReader"/> that .NET Framework 4.8 does not have, each doing what the
/// .NET 10 member of the same name does, so a call written for .NET 10 binds here unchanged.
/// </summary>
internal static class FrameworkStreamExtensions
{
    /// <summary>
    /// Reads into <paramref name="buffer"/> through the stream's own
    /// ReadAsync(byte[], int, int, CancellationToken), so a stream that overrides that member
    /// is read through its override. A buffer over an array is read into the array itself;
    /// any other is read into a pooled array and copied.
    /// </summary>
    internal static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (MemoryMarshal.TryGetArray<byte>(buffer, out var array))
            return new ValueTask<int>(stream.ReadAsync(array.Array!, array.Offset, array.Count, cancellationToken));

        var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        return FinishReadAsync(stream.ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer, buffer);

        static async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
        {
            try
            {
                var result = await readTask.ConfigureAwait(false);
                new ReadOnlySpan<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(localBuffer);
            }
        }
    }

    /// <summary>
    /// Writes <paramref name="buffer"/> through the stream's own
    /// WriteAsync(byte[], int, int, CancellationToken). A buffer over an array is written
    /// from the array itself; any other is copied into a pooled array first.
    /// </summary>
    internal static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (MemoryMarshal.TryGetArray(buffer, out var array))
            return new ValueTask(stream.WriteAsync(array.Array!, array.Offset, array.Count, cancellationToken));

        var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        buffer.Span.CopyTo(sharedBuffer);
        return new ValueTask(FinishWriteAsync(stream.WriteAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer));

        static async Task FinishWriteAsync(Task writeTask, byte[] localBuffer)
        {
            try
            {
                await writeTask.ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(localBuffer);
            }
        }
    }

    /// <summary>
    /// Reads until <paramref name="buffer"/> is full, however many reads that takes. A stream
    /// that ends first throws <see cref="EndOfStreamException"/>, an
    /// <see cref="IOException"/>, with .NET 10's English message.
    /// </summary>
    internal static void ReadExactly(this Stream stream, Span<byte> buffer)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = Read(stream, buffer.Slice(totalRead));
            if (read == 0)
                throw new EndOfStreamException("Unable to read beyond the end of the stream.");

            totalRead += read;
        }
    }

    /// <summary>
    /// One read into <paramref name="buffer"/> through the stream's
    /// Read(byte[], int, int), by way of a pooled array, as .NET 10's Stream reads a span.
    /// </summary>
    private static int Read(Stream stream, Span<byte> buffer)
    {
        var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            var numRead = stream.Read(sharedBuffer, 0, buffer.Length);
            if ((uint)numRead > (uint)buffer.Length)
                throw new IOException("Stream was too long.");

            new ReadOnlySpan<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
            return numRead;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }

    /// <summary>
    /// Reads the rest of the text, a block of characters at a time, looking at
    /// <paramref name="cancellationToken"/> before each block. A cancel ends the read at the
    /// next block with <see cref="TaskCanceledException"/>, as it does on .NET 10, where each
    /// block's read from the stream is handed the token; the text read until then is lost.
    /// </summary>
    internal static async Task<string> ReadToEndAsync(this TextReader reader, CancellationToken cancellationToken)
    {
        var text = new StringBuilder(4096);
        var chars = ArrayPool<char>.Shared.Rent(4096);
        try
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    await Task.FromCanceled(cancellationToken).ConfigureAwait(false);

                var length = await reader.ReadAsync(chars, 0, chars.Length).ConfigureAwait(false);
                if (length == 0) break;

                text.Append(chars, 0, length);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }

        return text.ToString();
    }
}
