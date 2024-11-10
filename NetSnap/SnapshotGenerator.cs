using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NetSnap
{
    public static class SnapshotGenerator
    {
        public static string CreateSnapshot(string sourcePath, string outputFile)
        {
            var ignoredDirectories = new[] { "bin", "obj" };
            var ignoredFiles = new[] { ".gitattributes", ".gitignore", "snapshot.txt" };
            var ignoredExtensions = new[] { ".img", ".jpeg", ".webp" };
            var projectFiles = Directory.GetFiles(sourcePath, "*.csproj", SearchOption.AllDirectories);

            if (!projectFiles.Any())
            {
                return "No .csproj files found in the specified directory.";
            }

            var builder = new StringBuilder("Project Snapshot:\n\n");

            foreach (var projectFile in projectFiles)
            {
                var projectDirectory = Path.GetDirectoryName(projectFile);
                builder.AppendLine($"### Project: {Path.GetFileName(projectFile)} ###\n");

                var files = Directory.GetFiles(projectDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(file => !IsInIgnoredDirectory(file, sourcePath, ignoredDirectories))
                    .Where(file => !IsIgnoredFile(file, sourcePath, ignoredFiles, outputFile))
                    .Where(file => !HasIgnoredExtension(file, ignoredExtensions))
                    .ToArray();

                if (files.Length != 0)
                {
                    foreach (var file in files)
                    {
                        AppendFileSnapshot(builder, sourcePath, file);
                    }
                }
                else
                {
                    builder.AppendLine("No relevant source files found for this project.\n");
                }

                builder.AppendLine($"### End of Project: {Path.GetFileName(projectFile)} ###\n");
            }

            return builder.ToString();
        }

        public static void AppendFileSnapshot(StringBuilder builder, string sourcePath, string file)
        {
            string relativePath = Path.GetRelativePath(sourcePath, file);
            builder.AppendLine($"File: {relativePath}");

            try
            {
                string content = File.ReadAllText(file);
                builder.AppendLine("-------- File Content --------")
                       .AppendLine(content)
                       .AppendLine("------------------------------\n");
            }
            catch (Exception ex)
            {
                builder.AppendLine($"Error reading file content: {ex.Message}\n");
            }
        }

        public static bool IsInIgnoredDirectory(string filePath, string sourcePath, string[] ignoredDirectories)
        {
            var relativePath = Path.GetRelativePath(sourcePath, filePath);

            if (relativePath.Split(Path.DirectorySeparatorChar)
                .Any(part => part.StartsWith(".") || ignoredDirectories.Contains(part)))
            {
                return true;
            }

            return false;
        }

        public static bool IsIgnoredFile(string filePath, string sourcePath, string[] ignoredFiles, string outputFile)
        {
            var fileName = Path.GetFileName(filePath);

            if (ignoredFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            if (filePath.Equals(outputFile, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static bool HasIgnoredExtension(string filePath, string[] ignoredExtensions)
        {
            var extension = Path.GetExtension(filePath);

            if (ignoredExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}