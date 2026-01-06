namespace NetSnap;

internal sealed class SplitTextWriter : TextWriter
{
    private readonly string _baseFilePath;
    private readonly long _maxBytes;
    private readonly Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private StreamWriter? _writer;
    private long _bytesWritten;
    private int _part = 1;

    public override Encoding Encoding => _encoding;

    public SplitTextWriter(string baseFilePath, long maxBytes)
    {
        if (string.IsNullOrWhiteSpace(baseFilePath)) throw new ArgumentException("Base file path is empty.", nameof(baseFilePath));

        if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes), "Max bytes must be > 0.");

        _baseFilePath = Path.GetFullPath(baseFilePath);
        _maxBytes = maxBytes;

        OpenPart(1);
    }

    public override void Write(char value) => Write(value.ToString());

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        // If a single write is bigger than max, we will chunk it.
        WriteChunked(value.AsSpan());
    }

    public override void Write(ReadOnlySpan<char> buffer)
    {
        if (buffer.IsEmpty) return;

        WriteChunked(buffer);
    }

    public override void Flush() => _writer?.Flush();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }

        base.Dispose(disposing);
    }

    private void WriteChunked(ReadOnlySpan<char> span)
    {
        while (span.IsEmpty is false)
        {
            EnsureWriter();

            var remaining = _maxBytes - _bytesWritten;

            if (remaining <= 0)
            {
                Rotate();

                continue;
            }

            // Find biggest prefix that fits into remaining bytes.
            // Binary search length in chars.
            var lo = 1;
            var hi = span.Length;
            var bestLen = 0;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) / 2);

                var bytes = _encoding.GetByteCount(span[..mid]);

                if (bytes <= remaining)
                {
                    bestLen = mid; lo = mid + 1;
                }
                else hi = mid - 1;   
            }

            // Edge case: cannot fit even 1 char (very small remaining) -> rotate.
            if (bestLen == 0)
            {
                Rotate();

                continue;
            }

            var chunk = span[..bestLen];

            _writer!.Write(chunk);
            _bytesWritten += _encoding.GetByteCount(chunk);

            span = span[bestLen..];
        }
    }

    private void EnsureWriter()
    {
        if (_writer is not null) return;

        OpenPart(_part);
    }

    private void Rotate()
    {
        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;

        _part++;

        OpenPart(_part);
    }

    private void OpenPart(int part)
    {
        var path = GetPartPath(part);

        var dir = Path.GetDirectoryName(path);

        if (string.IsNullOrWhiteSpace(dir) is false) Directory.CreateDirectory(dir);

        _writer = new StreamWriter(
            File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read),
            _encoding
        );

        _bytesWritten = 0;
    }

    private string GetPartPath(int part)
    {
        if (part <= 1) return _baseFilePath;

        var directory = Path.GetDirectoryName(_baseFilePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(_baseFilePath);
        var extension = Path.GetExtension(_baseFilePath);

        return Path.Combine(directory, $"{name}_{part}{extension}");
    }
}