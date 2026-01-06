CliOptions options;

try
{
    options = CliOptions.Parse(args);

    if (options.ShowHelp)
    {
        CliOptions.PrintHelp();

        return;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("Use --help to see usage.");

    return;
}

if (Directory.Exists(options.SourcePath) is false)
{
    Console.WriteLine($"Error: The path '{options.SourcePath}' does not exist.");

    return;
}

try
{
#if DEBUG
    Console.WriteLine("Running in DEBUG mode...");
#endif

    if (options.ByCsproj)
    {
        var outDir = options.ResolveOutputDirectory();

        Directory.CreateDirectory(outDir);

        var extraIgnoreDir = CliOptions.GetTopLevelDirNameIfInside(options.SourcePath, outDir);

        var projects = SnapshotGenerator.GetProjectFiles(options.SourcePath, extraIgnoreDir);

        if (projects.Length == 0)
        {
            Console.WriteLine("No .csproj files found in the specified directory.");

            return;
        }

        foreach (var csproj in projects)
        {
            var baseName = Path.GetFileNameWithoutExtension(csproj);
            var baseFile = Path.Combine(outDir, $"{baseName}.txt");

            using TextWriter writer = options.SplitMaxBytes is long max
                ? new SplitTextWriter(baseFile, max)
                : new StreamWriter(baseFile, append: false, new UTF8Encoding(false));

            SnapshotGenerator.WriteProjectSnapshot(
                sourcePath: options.SourcePath,
                projectFile: csproj,
                writer: writer,
                outputFileForIgnore: baseFile,
                extraIgnoredDirectory: extraIgnoreDir
            );

            writer.Flush();
        }

        Console.WriteLine($"Snapshots saved to {outDir}");
    }
    else
    {
        var outputFile = Path.GetFullPath(options.OutputPath);

        var outDir = Path.GetDirectoryName(outputFile) ?? options.SourcePath;

        var extraIgnoreDir = CliOptions.GetTopLevelDirNameIfInside(options.SourcePath, outDir);

        var projects = SnapshotGenerator.GetProjectFiles(options.SourcePath, extraIgnoreDir);

        if (projects.Length == 0)
        {
            Console.WriteLine("No .csproj files found in the specified directory.");

            return;
        }

        using TextWriter writer = options.SplitMaxBytes is long max
            ? new SplitTextWriter(outputFile, max)
            : new StreamWriter(outputFile, append: false, new UTF8Encoding(false));

        SnapshotGenerator.WriteAllProjectsSnapshot(
            sourcePath: options.SourcePath,
            projectFiles: projects,
            writer: writer,
            outputFileForIgnore: outputFile,
            extraIgnoredDirectory: extraIgnoreDir
        );

        writer.Flush();

        Console.WriteLine($"Snapshot saved to {outputFile}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}