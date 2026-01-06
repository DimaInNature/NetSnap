namespace NetSnap.Tests;

public sealed class SnapshotGeneratorTests
{
    [Fact]
    public void CreateSnapshot_ShouldReturnMessage_WhenNoCsprojFiles()
    {
        // Arrange
        var testDirectory = TestHelpers.CreateTestDirectory();

        // Act
        var result = SnapshotGenerator.CreateSnapshot(testDirectory, "output.txt");

        // Assert
        Assert.Equal("No .csproj files found in the specified directory.", result);

        // Cleanup
        Directory.Delete(testDirectory, true);
    }

    [Fact]
    public void CreateSnapshot_ShouldIncludeCsprojFiles_AndRelevantFiles()
    {
        // Arrange
        var testDirectory = TestHelpers.CreateTestDirectory();

        var csprojPath = Path.Combine(testDirectory, "TestProject.csproj");
        File.WriteAllText(csprojPath, "<Project></Project>");

        var sourceFile = Path.Combine(testDirectory, "Program.cs");
        File.WriteAllText(sourceFile, "class Program { static void Main() {} }");

        var ignoredFile = Path.Combine(testDirectory, "snapshot.txt");
        File.WriteAllText(ignoredFile, "Should be ignored");

        // Act
        var result = SnapshotGenerator.CreateSnapshot(testDirectory, "output.txt");

        // Assert
        Assert.Contains("TestProject.csproj", result);
        Assert.Contains("Program.cs", result);
        Assert.DoesNotContain("snapshot.txt", result);

        // Cleanup
        Directory.Delete(testDirectory, true);
    }

    [Fact]
    public void IsInIgnoredDirectory_ShouldReturnTrue_ForIgnoredDirectories()
    {
        // Arrange
        var sourcePath = "/test/project";
        var ignoredDirectories = new[] { "bin", "obj" };
        var filePath = Path.Combine(sourcePath, "bin", "debug", "test.dll");

        // Act
        var result = SnapshotGenerator.IsInIgnoredDirectory(filePath, sourcePath, ignoredDirectories);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsIgnoredFile_ShouldReturnTrue_ForIgnoredFileNames()
    {
        // Arrange
        var sourcePath = "/test/project";
        var ignoredFiles = new[] { ".gitattributes", ".gitignore" };
        var filePath = Path.Combine(sourcePath, ".gitignore");

        // Act
        var result = SnapshotGenerator.IsIgnoredFile(filePath, sourcePath, ignoredFiles, "output.txt");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasIgnoredExtension_ShouldReturnTrue_ForIgnoredExtensions()
    {
        // Arrange
        var ignoredExtensions = new[] { ".img", ".jpeg" };
        var filePath = "/test/project/image.jpeg";

        // Act
        var result = SnapshotGenerator.HasIgnoredExtension(filePath, ignoredExtensions);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetProjectFiles_ShouldIgnoreHiddenFolders_AndExtraIgnoredDirectory()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "A"));
            File.WriteAllText(Path.Combine(root, "A", "A.csproj"), "<Project></Project>");

            Directory.CreateDirectory(Path.Combine(root, ".hidden"));
            File.WriteAllText(Path.Combine(root, ".hidden", "Hidden.csproj"), "<Project></Project>");

            Directory.CreateDirectory(Path.Combine(root, "out"));
            File.WriteAllText(Path.Combine(root, "out", "Out.csproj"), "<Project></Project>");

            var projects = SnapshotGenerator.GetProjectFiles(root, extraIgnoredDirectory: "out");

            Assert.Single(projects);
            Assert.EndsWith("A.csproj", projects[0], StringComparison.OrdinalIgnoreCase);
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    [Fact]
    public void IsIgnoredFile_ShouldIgnoreOutputFile_AndSplitParts()
    {
        var root = TestHelpers.CreateTempDirectory();
        try
        {
            var ignoredFiles = new[] { ".gitattributes", ".gitignore", "snapshot.txt" };

            var outFull = Path.Combine(root, "snapshot.txt");

            Assert.True(SnapshotGenerator.IsIgnoredFile(outFull, root, ignoredFiles, outFull));

            var part2 = Path.Combine(root, "snapshot_2.txt");

            Assert.True(SnapshotGenerator.IsIgnoredFile(part2, root, ignoredFiles, outFull));

            var nonNumeric = Path.Combine(root, "snapshot_abc.txt");

            Assert.False(SnapshotGenerator.IsIgnoredFile(nonNumeric, root, ignoredFiles, outFull));
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    [Fact]
    public void CreateSnapshot_ShouldNotIncludeIgnoredExtensions()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(root, "Test.csproj"), "<Project></Project>");
            File.WriteAllText(Path.Combine(root, "Program.cs"), "class Program {}");
            File.WriteAllText(Path.Combine(root, "image.jpeg"), "fake");

            var result = SnapshotGenerator.CreateSnapshot(root, outputFile: Path.Combine(root, "out.txt"));

            Assert.Contains("Test.csproj", result);
            Assert.Contains("Program.cs", result);
            Assert.DoesNotContain("image.jpeg", result);
        }
        finally { TestHelpers.SafeDelete(root); }
    }

    [Fact]
    public void WriteAllProjectsSnapshot_ShouldWriteProjectHeaders()
    {
        var root = TestHelpers.CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(root, "P1.csproj"), "<Project></Project>");
            File.WriteAllText(Path.Combine(root, "P2.csproj"), "<Project></Project>");
            File.WriteAllText(Path.Combine(root, "Program.cs"), "class X {}");

            var projects = SnapshotGenerator.GetProjectFiles(root, extraIgnoredDirectory: null);

            Assert.Equal(2, projects.Length);

            var writer = new StringWriter();

            SnapshotGenerator.WriteAllProjectsSnapshot(
                sourcePath: root,
                projectFiles: projects,
                writer: writer,
                outputFileForIgnore: Path.Combine(root, "snapshot.txt"),
                extraIgnoredDirectory: null
            );

            var text = writer.ToString();

            Assert.Contains("### Project: P1.csproj ###", text);
            Assert.Contains("### Project: P2.csproj ###", text);
        }
        finally { TestHelpers.SafeDelete(root); }
    }
}