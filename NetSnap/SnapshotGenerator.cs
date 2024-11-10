namespace NetSnap;

public static class SnapshotGenerator
{
    private static readonly string[] DefaultIgnoredDirectories = ["bin", "obj"];

    private static readonly string[] DefaultIgnoredFiles = [".gitattributes", ".gitignore", "snapshot.txt"];

    private static readonly string[] DefaultIgnoredExtensions = [".img", ".jpeg", ".webp"];

    public static string CreateSnapshot(string sourcePath, string outputFile)
    {
        var projects = GetProjectFiles(sourcePath, extraIgnoredDirectory: null);

        if (projects.Length == 0)
            return "No .csproj files found in the specified directory.";

        var sb = new StringBuilder(capacity: 64 * 1024);

        using var sw = new StringWriter(sb);

        WriteAllProjectsSnapshot(
            sourcePath: sourcePath,
            projectFiles: projects,
            writer: sw,
            outputFileForIgnore: outputFile,
            extraIgnoredDirectory: null
        );

        return sb.ToString();
    }

    public static string[] GetProjectFiles(string sourcePath, string? extraIgnoredDirectory)
    {
        var ignoredDirs = extraIgnoredDirectory is null
            ? DefaultIgnoredDirectories
            : DefaultIgnoredDirectories.Concat([extraIgnoredDirectory]).ToArray();

        return Directory.GetFiles(sourcePath, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !IsInIgnoredDirectory(p, sourcePath, ignoredDirs))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void WriteAllProjectsSnapshot(
        string sourcePath,
        string[] projectFiles,
        TextWriter writer,
        string outputFileForIgnore,
        string? extraIgnoredDirectory)
    {
        writer.WriteLine("Project Snapshot:");
        writer.WriteLine();

        foreach (var projectFile in projectFiles)
        {
            WriteProjectSnapshot(
                sourcePath: sourcePath,
                projectFile: projectFile,
                writer: writer,
                outputFileForIgnore: outputFileForIgnore,
                extraIgnoredDirectory: extraIgnoredDirectory
            );
        }
    }

    public static void WriteProjectSnapshot(
        string sourcePath,
        string projectFile,
        TextWriter writer,
        string outputFileForIgnore,
        string? extraIgnoredDirectory)
    {
        var ignoredDirs = extraIgnoredDirectory is null
            ? DefaultIgnoredDirectories
            : DefaultIgnoredDirectories.Concat([extraIgnoredDirectory]).ToArray();

        var projectDirectory = Path.GetDirectoryName(projectFile);
        if (string.IsNullOrWhiteSpace(projectDirectory))
            return;

        writer.WriteLine($"### Project: {Path.GetFileName(projectFile)} ###");
        writer.WriteLine();

        var files = Directory.GetFiles(projectDirectory, "*.*", SearchOption.AllDirectories)
            .Where(file => !IsInIgnoredDirectory(file, sourcePath, ignoredDirs))
            .Where(file => !IsIgnoredFile(file, sourcePath, DefaultIgnoredFiles, outputFileForIgnore))
            .Where(file => !HasIgnoredExtension(file, DefaultIgnoredExtensions))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length != 0)
        {
            foreach (var file in files)
            {
                WriteFileSnapshot(writer, sourcePath, file);
            }
        }
        else
        {
            writer.WriteLine("No relevant source files found for this project.");
            writer.WriteLine();
        }

        writer.WriteLine($"### End of Project: {Path.GetFileName(projectFile)} ###");
        writer.WriteLine();
    }

    private static void WriteFileSnapshot(TextWriter writer, string sourcePath, string file)
    {
        string relativePath = Path.GetRelativePath(sourcePath, file);
        writer.WriteLine($"File: {relativePath}");

        try
        {
            writer.WriteLine("-------- File Content --------");

            // Stream content line-by-line to avoid holding huge files in memory.
            using var sr = new StreamReader(File.OpenRead(file), detectEncodingFromByteOrderMarks: true);

            string? line;
            while ((line = sr.ReadLine()) is not null)
                writer.WriteLine(line);

            writer.WriteLine("------------------------------");
            writer.WriteLine();
        }
        catch (Exception ex)
        {
            writer.WriteLine($"Error reading file content: {ex.Message}");
            writer.WriteLine();
        }
    }

    // Kept for backwards compatibility (tests, external calls)
    public static void AppendFileSnapshot(StringBuilder builder, string sourcePath, string file)
    {
        using var sw = new StringWriter(builder);
        WriteFileSnapshot(sw, sourcePath, file);
    }

    public static bool IsInIgnoredDirectory(string filePath, string sourcePath, string[] ignoredDirectories)
    {
        var relativePath = Path.GetRelativePath(sourcePath, filePath);

        return relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part =>
                part.StartsWith(".", StringComparison.Ordinal) ||
                ignoredDirectories.Contains(part, StringComparer.OrdinalIgnoreCase));
    }

    public static bool IsIgnoredFile(string filePath, string sourcePath, string[] ignoredFiles, string outputFile)
    {
        var fileName = Path.GetFileName(filePath);

        if (ignoredFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(outputFile))
        {
            var outFull = Path.GetFullPath(outputFile);
            var inFull = Path.GetFullPath(filePath);

            if (inFull.Equals(outFull, StringComparison.OrdinalIgnoreCase))
                return true;

            // Ignore split parts: output.txt, output_2.txt, output_3.txt, ...
            var baseName = Path.GetFileNameWithoutExtension(outFull);
            var ext = Path.GetExtension(outFull);
            if (!string.IsNullOrEmpty(baseName) && !string.IsNullOrEmpty(ext))
            {
                if (fileName.StartsWith(baseName + "_", StringComparison.OrdinalIgnoreCase) &&
                    fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    var between = fileName.Substring(
                        (baseName.Length + 1),
                        fileName.Length - (baseName.Length + 1) - ext.Length);

                    if (between.Length > 0 && between.All(char.IsDigit))
                        return true;
                }
            }
        }

        return false;
    }

    public static bool HasIgnoredExtension(string filePath, string[] ignoredExtensions)
    {
        var extension = Path.GetExtension(filePath);

        return ignoredExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
