namespace LessonC09.Web.Lessons;

/// <summary>The lesson implementation embedded at build time, so the page always shows the code that actually runs.</summary>
public static class LessonSource
{
    public static readonly string Text = Load();

    private static string Load()
    {
        using var stream = typeof(LessonSource).Assembly.GetManifestResourceStream("lesson-source.cs")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
