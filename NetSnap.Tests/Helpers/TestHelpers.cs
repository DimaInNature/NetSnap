namespace NetSnap.Tests.Helpers;

internal static class TestHelpers
{
    public static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "NetSnap.Tests", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        return directory;
    }

    public static string CreateTestDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Directory.CreateDirectory(tempPath);

        return tempPath;
    }

    public static void SafeDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { }
    }
}