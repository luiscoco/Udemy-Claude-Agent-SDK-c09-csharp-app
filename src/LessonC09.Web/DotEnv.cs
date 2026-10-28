namespace LessonC09.Web;

public static class AppPaths
{
    /// <summary>The folder holding LessonC09.sln, where .env and .lab-data live (falls back to the working directory).</summary>
    public static readonly string Root = Find();

    private static string Find()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "LessonC09.sln"))) return dir.FullName;
        return Directory.GetCurrentDirectory();
    }
}

/// <summary>Loads KEY=value lines into the process environment without overriding variables already set.</summary>
public static class DotEnv
{
    public static void Load(string path)
    {
        if (!File.Exists(path)) return;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var split = line.IndexOf('=');
            if (split <= 0) continue;
            var key = line[..split].Trim();
            var value = line[(split + 1)..].Trim().Trim('"', '\'');
            if (Environment.GetEnvironmentVariable(key) is null) Environment.SetEnvironmentVariable(key, value);
        }
    }
}
