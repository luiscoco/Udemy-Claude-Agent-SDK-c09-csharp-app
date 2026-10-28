using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LessonC09.Web.Lessons;

[JsonConverter(typeof(JsonStringEnumConverter<Outcome>))]
public enum Outcome
{
    [JsonStringEnumMemberName("passed")] Passed,
    [JsonStringEnumMemberName("rejected")] Rejected,
    [JsonStringEnumMemberName("paused")] Paused,
    [JsonStringEnumMemberName("conflict")] Conflict,
    [JsonStringEnumMemberName("incomplete")] Incomplete,
    [JsonStringEnumMemberName("unconfirmed")] Unconfirmed,
}

public sealed record LessonResult(Outcome Outcome, string Summary, JsonObject Evidence, IReadOnlyList<string> Steps)
{
    public string Source => "local-experiment";
}

public sealed record Scenario(string Id, string Label, JsonObject Input, Outcome Expected, JsonObject? Checks = null);

/// <summary>Invalid experiment input; reported to the student as a 400, never as a crash.</summary>
public sealed class LessonInputException(string message) : Exception(message);

public static class LessonInput
{
    public static JsonArray List(JsonObject input, string key)
    {
        if (input[key] is not JsonArray value || value.Count > 200)
            throw new LessonInputException($"{key} must be a list of at most 200 values");
        return value;
    }

    public static JsonObject Object(JsonNode? value) =>
        value as JsonObject ?? throw new LessonInputException("Expected an object");

    public static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
