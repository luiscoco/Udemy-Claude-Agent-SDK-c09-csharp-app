using System.Text.Json;
using System.Text.Json.Nodes;
using LessonC09.Web.Lessons;
using LessonC09.Web.Live;

namespace LessonC09.Tests;

public class LessonTests
{
    public static TheoryData<string> ScenarioIds => [.. StreamRecoveryLesson.Cases.Select(c => c.Id)];

    [Theory]
    [MemberData(nameof(ScenarioIds))]
    public void Scenario_matches_expected_outcome_and_evidence(string id)
    {
        var scenario = StreamRecoveryLesson.Cases.Single(c => c.Id == id);
        var result = StreamRecoveryLesson.Run(scenario.Input);

        Assert.Equal(scenario.Expected, result.Outcome);
        foreach (var (key, expected) in scenario.Checks ?? [])
            Assert.True(JsonNode.DeepEquals(expected, result.Evidence[key]), $"{key}: expected {expected}, got {result.Evidence[key]}");
    }

    [Fact]
    public void Previews_never_suppress_persisted_ids()
    {
        var result = StreamRecoveryLesson.Run(JsonNode.Parse("""{"historyIds": ["e1"], "liveIds": [], "previewChunks": ["e1"]}""")!.AsObject());
        Assert.Equal(1, (int)result.Evidence["delivered"]!);
        Assert.Equal(0, (int)result.Evidence["duplicates"]!);
        Assert.Equal("e1", (string)result.Evidence["preview"]!);
    }

    [Theory]
    [InlineData("""{"historyIds": [""], "liveIds": [], "previewChunks": []}""")]
    [InlineData("""{"historyIds": [1], "liveIds": [], "previewChunks": []}""")]
    [InlineData("""{"historyIds": "e1", "liveIds": [], "previewChunks": []}""")]
    public void Invalid_input_is_rejected(string json) =>
        Assert.Throws<LessonInputException>(() => StreamRecoveryLesson.Run(JsonNode.Parse(json)!.AsObject()));

    [Fact]
    public void Observer_recovers_missed_history_without_duplicating_streamed_events()
    {
        var observer = new SessionObserver();
        Assert.False(observer.Deliver(Event("""{"id": "u1", "type": "user.message"}""")));
        Assert.True(observer.Deliver(Event("""{"id": "s1", "type": "session.status_idle", "stop_reason": {"type": "end_turn"}}""")));

        // History contains the agent message the stream missed, plus events already seen.
        observer.Recover(Event("""{"id": "u1", "type": "user.message"}"""));
        observer.Recover(Event("""{"id": "a1", "type": "agent.message", "content": [{"type": "text", "text": "Recovered"}]}"""));
        observer.Recover(Event("""{"id": "s1", "type": "session.status_idle", "stop_reason": {"type": "end_turn"}}"""));

        Assert.Equal("Recovered", observer.Answer);
        Assert.Equal("end_turn", observer.StopReason);
        Assert.Equal(1, observer.Recovered);
        Assert.Equal(["user.message", "session.status_idle", "agent.message"], observer.EventTypes);
    }

    [Fact]
    public void Markdown_answer_is_formatted_but_cannot_inject_markup()
    {
        var html = SafeMarkdown.ToHtml("## Title\nUses *before* and `e1`.\n\n<script>alert(1)</script> [bad](javascript:alert(1)) [ok](https://example.com)");

        Assert.Contains("<h2>Title</h2>", html);
        Assert.Contains("<em>before</em>", html);
        Assert.Contains("<code>e1</code>", html);
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("javascript:", html);
        Assert.Contains("href=\"https://example.com\"", html);
    }

    private static JsonElement Event(string json) => JsonDocument.Parse(json).RootElement;
}
