namespace NetSnap.Tests;

public sealed class CliOptionsTests
{
    [Fact]
    public void Parse_ShouldSetShowHelp_WhenHelpFlagsProvided()
    {
        var a = CliOptions.Parse(["--help"]);
        Assert.True(a.ShowHelp);

        var b = CliOptions.Parse(["-h"]);
        Assert.True(b.ShowHelp);

        var c = CliOptions.Parse(["/?"]);
        Assert.True(c.ShowHelp);
    }

    [Fact]
    public void Parse_ShouldSetByCsproj_WhenFlagProvided()
    {
        var options = CliOptions.Parse(["C:\\src", "out", "--by-csproj"]);

        Assert.True(options.ByCsproj);
        Assert.False(options.ShowHelp);
        Assert.Equal("C:\\src", options.SourcePath);
        Assert.Equal("out", options.OutputPath);
    }

    [Theory]
    [InlineData("--split", "5MB", 5L * 1024 * 1024)]
    [InlineData("--split", "500KB", 500L * 1024)]
    [InlineData("--split=1GB", null, 1L * 1024 * 1024 * 1024)]
    [InlineData("--split=500000", null, 500000L)]
    [InlineData("--split", "123B", 123L)]
    public void Parse_ShouldParseSplitSizes(string firstArg, string? secondArg, long expectedBytes)
    {
        string[] args = secondArg is null
            ? ["C:\\src", "snapshot.txt", firstArg]
            : ["C:\\src", "snapshot.txt", firstArg, secondArg];

        var opt = CliOptions.Parse(args);

        Assert.Equal(expectedBytes, opt.SplitMaxBytes);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenSplitMissingValue()
    {
        var ex = Assert.Throws<ArgumentException>(() => CliOptions.Parse(["C:\\src", "snapshot.txt", "--split"]));

        Assert.Contains("Missing value for --split", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--split", "0")]
    [InlineData("--split", "-1")]
    public void Parse_ShouldThrow_WhenSplitIsNotPositive(string flag, string value)
    {
        var ex = Assert.Throws<ArgumentException>(() => CliOptions.Parse(["C:\\src", "snapshot.txt", flag, value]));

        Assert.Contains("Invalid --split size", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenUnknownOption()
    {
        var ex = Assert.Throws<ArgumentException>(() => CliOptions.Parse(["C:\\src", "snapshot.txt", "--nope"]));

        Assert.Contains("Unknown option", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveOutputDirectory_ShouldReturnDirectory_WhenOutputIsExistingDirectory()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            var options = new CliOptions
            {
                SourcePath = root,
                OutputPath = root,
                SplitMaxBytes = null,
                ByCsproj = false,
                ShowHelp = false
            };

            var resolved = options.ResolveOutputDirectory();

            Assert.Equal(Path.GetFullPath(root), resolved);
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    [Fact]
    public void ResolveOutputDirectory_ShouldTreatPathWithoutExtension_AsDirectory()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            var outDirectory = Path.Combine(root, "out");

            var options = new CliOptions
            {
                SourcePath = root,
                OutputPath = outDirectory,
                SplitMaxBytes = null,
                ByCsproj = false,
                ShowHelp = false
            };

            var resolved = options.ResolveOutputDirectory();

            Assert.Equal(Path.GetFullPath(outDirectory), resolved);
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    [Fact]
    public void ResolveOutputDirectory_ShouldReturnParentDirectory_WhenOutputLooksLikeFile()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            var outFile = Path.Combine(root, "snap.txt");

            var options = new CliOptions
            {
                SourcePath = root,
                OutputPath = outFile,
                SplitMaxBytes = null,
                ByCsproj = false,
                ShowHelp = false
            };

            var resolved = options.ResolveOutputDirectory();

            Assert.Equal(Path.GetFullPath(root), resolved);
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    [Fact]
    public void IsPathInside_ShouldWork_ForInsideAndOutside()
    {
        var root = TestHelpers.CreateTempDirectory();
        var inside = Path.Combine(root, "sub", "x");
        var outside = TestHelpers.CreateTempDirectory();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(inside)!);

            Assert.True(CliOptions.IsPathInside(root, inside));
            Assert.False(CliOptions.IsPathInside(root, outside));
        }
        finally
        {
            TestHelpers.SafeDelete(root);
            TestHelpers.SafeDelete(outside);
        }
    }

    [Fact]
    public void GetTopLevelDirNameIfInside_ShouldReturnFirstSegment()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            var outDirectory = Path.Combine(root, "out", "inner");
            Directory.CreateDirectory(outDirectory);

            var top = CliOptions.GetTopLevelDirNameIfInside(root, outDirectory);
            Assert.Equal("out", top);

            var rootItself = CliOptions.GetTopLevelDirNameIfInside(root, root);
            Assert.Null(rootItself);

            var outside = TestHelpers.CreateTempDirectory();

            try { Assert.Null(CliOptions.GetTopLevelDirNameIfInside(root, outside)); }

            finally { TestHelpers.SafeDelete(outside); }
        }
        finally { TestHelpers.SafeDelete(root); }
    }
}