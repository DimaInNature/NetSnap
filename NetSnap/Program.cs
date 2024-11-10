using System;
using System.IO;

namespace NetSnap
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var sourcePath = args.Length > 0 ? args[0] : GetSourcePath();
            var outputFile = args.Length > 1 ? args[1] : Path.Combine(sourcePath, "snapshot.txt");

            if (!Directory.Exists(sourcePath))
            {
                Console.WriteLine($"Error: The path '{sourcePath}' does not exist.");
                return;
            }

            try
            {
#if DEBUG
                Console.WriteLine("Running in DEBUG mode...");
#endif
                var snapshot = SnapshotGenerator.CreateSnapshot(sourcePath, outputFile);
                File.WriteAllText(outputFile, snapshot);

                Console.WriteLine($"Snapshot saved to {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static string GetSourcePath()
        {
#if DEBUG
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
#else
            return Directory.GetCurrentDirectory();
#endif
        }
    }
}