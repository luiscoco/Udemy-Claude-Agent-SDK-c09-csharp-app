using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Models.Beta.Agents;
using Anthropic.Models.Beta.Environments;

namespace LessonC09.Web.Live;

/// <summary>Optional one-time provisioning (<c>dotnet run -- setup</c>). Reuses IDs already present in .env.</summary>
public static class ResourceSetup
{
    public static async Task RunAsync(string envFile)
    {
        static bool Valid(string? value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("your_");
        if (!Valid(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            throw new InvalidOperationException("Set your own ANTHROPIC_API_KEY in .env first.");

        AnthropicClient client = new() { MaxRetries = 0 };
        var contents = File.Exists(envFile) ? await File.ReadAllTextAsync(envFile) : "";
        async Task Save(string key, string value)
        {
            var pattern = new Regex($@"^\s*{key}=.*$", RegexOptions.Multiline);
            contents = pattern.IsMatch(contents) ? pattern.Replace(contents, $"{key}={value}", 1) : contents.TrimEnd() + $"\n{key}={value}\n";
            await File.WriteAllTextAsync(envFile, contents);
        }

        if (!Valid(Environment.GetEnvironmentVariable("CLAUDE_MANAGED_ENVIRONMENT_ID")))
        {
            var environment = await client.Beta.Environments.Create(new EnvironmentCreateParams
            {
                Name = "C09 student environment",
                Config = new BetaCloudConfigParams { Networking = new BetaLimitedNetworkParams() },
            });
            // Save immediately so a later agent failure does not lose the environment ID.
            await Save("CLAUDE_MANAGED_ENVIRONMENT_ID", environment.ID);
        }
        if (!Valid(Environment.GetEnvironmentVariable("CLAUDE_MANAGED_AGENT_ID")))
        {
            var model = Environment.GetEnvironmentVariable("CLAUDE_MODEL");
            var agent = await client.Beta.Agents.Create(new AgentCreateParams
            {
                Name = "C09 text-only student agent",
                Model = new BetaManagedAgentsModelConfigParams { ID = string.IsNullOrWhiteSpace(model) ? "claude-opus-5-5" : model },
                System = "Answer the student's question clearly and briefly.",
                Tools = [], McpServers = [], Skills = [],
            });
            await Save("CLAUDE_MANAGED_AGENT_ID", agent.ID);
        }
        Console.WriteLine("Prerequisite IDs are saved in your local .env. Existing configured IDs were reused.");
    }
}
