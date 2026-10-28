using System.Text.Json;
using System.Text.Json.Nodes;
using LessonC09.Web.Live;

namespace LessonC09.Web.Lessons;

public sealed class LabBusyException() : Exception("An experiment is already running. Wait for it to finish.");

public sealed record JournalEntry(Guid Id, DateTimeOffset At, int Lesson, LessonResult Result);

/// <summary>
/// Runs one experiment at a time, shared by the Blazor page and the JSON API.
/// The run journal is separate from experiment state.
/// </summary>
public sealed class LabRunner(ManagedAgentCommentary live)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim busy = new(1, 1);
    private static string JournalPath => Path.Combine(AppPaths.Root, ".lab-data", "runs.jsonl");

    public bool LiveConfigured => live.Configured;

    public Task<JournalEntry> RunLocalAsync(JsonObject input, CancellationToken ct = default) =>
        Exclusive(async () =>
        {
            var entry = new JournalEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, StreamRecoveryLesson.Number, StreamRecoveryLesson.Run(input));
            Directory.CreateDirectory(Path.GetDirectoryName(JournalPath)!);
            await File.AppendAllTextAsync(JournalPath, JsonSerializer.Serialize(entry, Json) + "\n", ct);
            return entry;
        });

    public Task<LiveCommentary> RunLiveAsync(JsonObject input, CancellationToken ct = default) =>
        Exclusive(() => live.ExplainAsync(StreamRecoveryLesson.Title, StreamRecoveryLesson.Run(input), ct));

    private async Task<T> Exclusive<T>(Func<Task<T>> work)
    {
        if (!busy.Wait(0)) throw new LabBusyException();
        try { return await work(); }
        finally { busy.Release(); }
    }
}
