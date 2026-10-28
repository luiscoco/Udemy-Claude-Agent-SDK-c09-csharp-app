using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LessonC09.Web.Live;

/// <summary>
/// Renders model text as Markdown without trusting it: raw HTML is escaped and only
/// http(s)/mailto links survive, so an answer cannot inject markup or script URLs.
/// </summary>
public static class SafeMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().DisableHtml().Build();

    public static string ToHtml(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        foreach (var link in document.Descendants<LinkInline>())
            if (!IsSafe(link.Url)) { link.Url = null; link.IsImage = false; }
        return document.ToHtml(Pipeline);
    }

    private static bool IsSafe(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto";
}
