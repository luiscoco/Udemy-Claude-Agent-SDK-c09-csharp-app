using System.Text.Json;
using System.Text.Json.Nodes;

namespace LessonC09.Web.Lessons;

// A focused local experiment derived from this lesson's reviewed presentation.
public static class StreamRecoveryLesson
{
    public const int Number = 9;
    public const string Title = "Stream Recovery without Lost Events";
    public const string Description = "The live event stream has no replay. If your application sends a prompt before establishing its stream, early events can be missed by real-time handling.";
    public const string Scope = "Executes a recovery reducer over authored history/live records. No reconnect or remote mutation is claimed by the local experiment. Each run starts from a fresh local seed. The run journal stores results, not production workflow state.";
    public static readonly IReadOnlyList<string> Concepts = ["Stream before history", "Persisted-ID deduplication", "Recovery does not replay mutations"];

    // Returned fresh on every access so an edited input never leaks into the next run.
    public static IReadOnlyList<Scenario> Cases =>
    [
        new("0", "Recover overlapping delivery",
            Json("""{"historyIds": ["e1", "e2"], "liveIds": ["e2", "e3"], "previewChunks": ["Hello", " world"]}"""),
            Outcome.Passed, Json("""{"duplicates": 1, "delivered": 3}""")),
        new("1", "Deduplicate repeated live delivery",
            Json("""{"historyIds": ["e1"], "liveIds": ["e1", "e1"], "previewChunks": ["A", "B"]}"""),
            Outcome.Passed, Json("""{"duplicates": 2, "delivered": 1}""")),
    ];

    public static LessonResult Run(JsonObject input)
    {
        var seen = new HashSet<string>();
        var delivered = new List<string>();
        var trace = new List<string>();
        var duplicates = 0;
        trace.Add("Open and buffer live observation before reading history");
        foreach (var node in LessonInput.List(input, "historyIds").Concat(LessonInput.List(input, "liveIds")))
        {
            if (node is not JsonValue value || !value.TryGetValue(out string? id) || id.Length == 0)
                throw new LessonInputException("Every persisted record needs an ID");
            if (seen.Contains(id)) { duplicates++; continue; }
            delivered.Add(id);
            seen.Add(id);
        }
        var preview = string.Concat(LessonInput.List(input, "previewChunks").Select(Display));
        return new LessonResult(Outcome.Passed, "Recovered records without replaying session creation or user input",
            new JsonObject
            {
                ["duplicates"] = duplicates,
                ["delivered"] = delivered.Count,
                ["ids"] = new JsonArray([.. delivered.Select(id => JsonValue.Create(id))]),
                ["preview"] = preview,
                ["mutationReplays"] = 0,
            },
            [.. trace, "Process previews separately from persisted identities", "Deduplicate canonical records after successful handling"]);
    }

    // Preview chunks are display text only; they never enter the persisted-ID set.
    private static string Display(JsonNode? node) => node switch
    {
        null => "null",
        JsonValue v when v.GetValueKind() == JsonValueKind.String => v.GetValue<string>(),
        _ => node.ToJsonString(),
    };

    private static JsonObject Json(string json) => JsonNode.Parse(json)!.AsObject();
}
