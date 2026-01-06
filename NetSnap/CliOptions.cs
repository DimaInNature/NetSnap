namespace NetSnap;

internal sealed class CliOptions
{
    public required string SourcePath { get; init; }

    public required string OutputPath { get; init; }

    public long? SplitMaxBytes { get; init; }

    public bool ByCsproj { get; init; }

    public bool ShowHelp { get; init; }

    public static CliOptions Parse(string[] args)
    {
        var showHelp = false;

        var byCsproj = false;

        long? splitBytes = null;

        var positional = new List<string>(capacity: 2);

        for (int i = 0; i < args.Length; i++)
        {
            var argument = args[i];

            if (string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(argument, "/?", StringComparison.OrdinalIgnoreCase))
            {
                showHelp = true;

                continue;
            }

            if (string.Equals(argument, "--by-csproj", StringComparison.OrdinalIgnoreCase))
            {
                byCsproj = true;

                continue;
            }

            if (argument.StartsWith("--split", StringComparison.OrdinalIgnoreCase))
            {

                // --split=5MB
                var eq = argument.IndexOf('=');

                string sizeArg;

                if (eq >= 0)
                {
                    sizeArg = argument[(eq + 1)..].Trim();
                }
                else
                {
                    // --split 5MB
                    if (i + 1 >= args.Length) throw new ArgumentException("Missing value for --split. Example: --split 5MB");

                    sizeArg = args[++i];
                }

                splitBytes = ParseSizeToBytes(sizeArg);

                if (splitBytes <= 0) throw new ArgumentException("Invalid --split size. Must be > 0.");

                continue;
            }

            if (argument.StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unknown option: {argument}");
            
            positional.Add(argument);
        }

        var sourcePath = positional.Count > 0 ? positional[0] : GetDefaultSourcePath();
        var outputPath = positional.Count > 1 ? positional[1] : Path.Combine(sourcePath, "snapshot.txt");

        return new CliOptions
        {
            SourcePath = sourcePath,
            OutputPath = outputPath,
            SplitMaxBytes = splitBytes,
            ByCsproj = byCsproj,
            ShowHelp = showHelp
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
        NetSnap - create a snapshot of .NET project structure and file contents
        
        Usage:
        
        netsnap [sourcePath] [outputPathOrDir] [--split <size>] [--by-csproj]
        
        Options:
        --split <size> Split output into multiple files of max <size>.
        Examples: 500000, 500KB, 5MB, 1GB
        
        --by-csproj Create separate output per *.csproj.
        Output files are named after csproj:
        MyProject.txt, MyProject_2.txt, ...
        
        --help, -h, /?     Show help.
        
        Examples:
        
        netsnap . snapshot.txt

        netsnap . snapshot.txt --split 5MB
        
        netsnap . out --by-csproj
        
        netsnap . out --by-csproj --split 2MB
        """);
    }

    public string ResolveOutputDirectory()
    {
        if (Directory.Exists(OutputPath)) return Path.GetFullPath(OutputPath);

        var extension = Path.GetExtension(OutputPath);

        if (string.IsNullOrEmpty(extension)) return Path.GetFullPath(OutputPath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(OutputPath));

        return string.IsNullOrWhiteSpace(directory) ? Path.GetFullPath(Directory.GetCurrentDirectory()) : directory!;
    }

    public static bool IsPathInside(string root, string candidate)
    {
        var rootFull = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        var candFull = Path.GetFullPath(candidate)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return candFull.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetTopLevelDirNameIfInside(string sourceRoot, string dirToCheck)
    {
        if (IsPathInside(sourceRoot, dirToCheck) is false) return null;

        var relativePath = Path.GetRelativePath(sourceRoot, dirToCheck);

        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".") return null;

        var firstSep = relativePath.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);

        return firstSep >= 0 ? relativePath[..firstSep] : relativePath;
    }

    private static long ParseSizeToBytes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Empty size value for --split.");

        var trimmed = value.Trim();

        // pure bytes
        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytesOnly)) return bytesOnly;

        trimmed = trimmed.Replace(" ", "", StringComparison.Ordinal);

        var upper = trimmed.ToUpperInvariant();

        var multiplier =
            upper.EndsWith("KB") ? 1024L :
            upper.EndsWith("MB") ? 1024L * 1024L :
            upper.EndsWith("GB") ? 1024L * 1024L * 1024L :
            upper.EndsWith("B") ? 1L :
            0;

        if (multiplier == 0)
            throw new ArgumentException($"Unsupported size suffix in '{value}'. Use KB/MB/GB or bytes.");

        var numPart = upper.EndsWith("KB") || upper.EndsWith("MB") || upper.EndsWith("GB")
            ? upper[..^2]
            : upper[..^1];

        if (long.TryParse(numPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) is false)
            throw new ArgumentException($"Invalid size '{value}'.");

        checked { return n * multiplier; }
    }

    private static string GetDefaultSourcePath()
    {
#if DEBUG
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
#else
        return Directory.GetCurrentDirectory();
#endif
    }
}