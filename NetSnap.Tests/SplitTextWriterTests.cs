namespace NetSnap.Tests;

public sealed class SplitTextWriterTests
{
    [Fact]
    public void Ctor_ShouldThrow_OnInvalidArgs()
    {
        Assert.Throws<ArgumentException>(() => new SplitTextWriter("   ", 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SplitTextWriter("x.txt", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SplitTextWriter("x.txt", -1));
    }

    [Fact]
    public void Write_ShouldNotSplit_WhenContentFits()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            var baseFile = Path.Combine(root, "snapshot.txt");

            var content = new string('a', 100);

            using (var writer = new SplitTextWriter(baseFile, maxBytes: 10_000))
            {
                writer.Write(content);
            }

            Assert.True(File.Exists(baseFile));
            Assert.False(File.Exists(Path.Combine(root, "snapshot_2.txt")));

            var read = File.ReadAllText(baseFile, new UTF8Encoding(false));

            Assert.Equal(content, read);
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    [Fact]
    public void Write_ShouldSplit_AndPreserveExactContent()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            var baseFile = Path.Combine(root, "snapshot.txt");

            var maxBytes = 50;

            var content = new string('a', 140);

            using (var writer = new SplitTextWriter(baseFile, maxBytes))
            {
                writer.Write(content);
            }

            var parts = EnumerateParts(baseFile).ToList();

            Assert.True(parts.Count >= 2);

            foreach (var part in parts)
            {
                var length = new FileInfo(part).Length;

                Assert.True(length <= maxBytes, $"Part '{part}' is {length} bytes, expected <= {maxBytes}.");
            }

            var bytes = parts.SelectMany(File.ReadAllBytes).ToArray();

            var reconstructed = new UTF8Encoding(false).GetString(bytes);

            Assert.Equal(content, reconstructed);
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    [Fact]
    public void Write_ShouldSplitAcrossMultipleWriteCalls()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            var baseFile = Path.Combine(root, "snapshot.txt");

            var maxBytes = 64;

            var p1 = new string('x', 60);
            var p2 = new string('y', 60);
            var p3 = new string('z', 60);

            var content = p1 + p2 + p3;

            using (var writer = new SplitTextWriter(baseFile, maxBytes))
            {
                writer.Write(p1);
                writer.Write(p2);
                writer.Write(p3);
            }

            var parts = EnumerateParts(baseFile).ToList();

            Assert.True(parts.Count >= 2);

            var bytes = parts.SelectMany(File.ReadAllBytes).ToArray();

            var reconstructed = new UTF8Encoding(false).GetString(bytes);

            Assert.Equal(content, reconstructed);
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    private static IEnumerable<string> EnumerateParts(string baseFilePath)
    {
        var fullPath = Path.GetFullPath(baseFilePath);
        var directoryName = Path.GetDirectoryName(fullPath)!;
        var name = Path.GetFileNameWithoutExtension(fullPath);
        var extension = Path.GetExtension(fullPath);

        var first = Path.Combine(directoryName, name + extension);

        if (File.Exists(first)) yield return first;

        for (int i = 2; ; i++)
        {
            var path = Path.Combine(directoryName, $"{name}_{i}{extension}");

            if (File.Exists(path) is false) yield break;

            yield return path;
        }
    }
}
