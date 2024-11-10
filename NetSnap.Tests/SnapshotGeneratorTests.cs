using System;
using System.IO;
using Xunit;

namespace NetSnap.Tests
{
    public class SnapshotGeneratorTests
    {
        [Fact]
        public void CreateSnapshot_ShouldReturnMessage_WhenNoCsprojFiles()
        {
            // Arrange
            var testDirectory = CreateTestDirectory();

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
            var testDirectory = CreateTestDirectory();
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

        private static string CreateTestDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }
    }
}
