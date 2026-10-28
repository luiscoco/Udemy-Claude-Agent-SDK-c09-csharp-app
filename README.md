# C09 · Stream Recovery without Lost Events (Blazor · .NET 10)

C# port of `../lesson-c09-app` (Node/TypeScript). Same lesson, scenarios, student pages and optional Managed Agents live commentary, rebuilt as a Blazor Web App (Interactive Server) on .NET 10 using the official [Anthropic C# SDK](https://www.nuget.org/packages/Anthropic) 13.3.0.

Executes a recovery reducer over authored history/live records. No reconnect or remote mutation is claimed by the local experiment. Each run starts from a fresh local seed. The run journal stores results, not production workflow state.

## Concepts students will learn

- Stream before history
- Persisted-ID deduplication
- Recovery does not replay mutations

## Configure and run

Use the .NET 10 SDK. In PowerShell, from this folder:

```powershell
dotnet --version
dotnet restore
Copy-Item .env.example .env
notepad .env
dotnet run --project src/LessonC09.Web
```

Open `http://127.0.0.1:3009` (or the `PORT` from `.env`). Stop with Ctrl+C.

No API key is required for the local experiment. For the optional live commentary, set `ANTHROPIC_API_KEY`, then create resource IDs if you don't have them yet:

```powershell
dotnet run --project src/LessonC09.Web -- setup
```

This creates the environment before a text-only agent, saves each ID to `.env` immediately, and reuses IDs already configured. It uses `CLAUDE_MODEL` or the default `claude-opus-5-5`. An uncertain network failure may have created a resource: inspect your account before retrying. Restart the server after editing `.env`.

## Layout

| Path | Port of | Purpose |
|---|---|---|
| `src/LessonC09.Web/Lessons/StreamRecoveryLesson.cs` | `src/lesson.ts` | Scenarios and the recovery reducer |
| `src/LessonC09.Web/Lessons/LessonModels.cs` | `src/core.ts` | Outcome, result, scenario and input helpers |
| `src/LessonC09.Web/Lessons/LabRunner.cs` | `src/server.ts` (run path) | One experiment at a time; appends to `.lab-data/runs.jsonl` |
| `src/LessonC09.Web/Live/ManagedAgentCommentary.cs` | `src/provider.ts` | Text-only Managed Agents session via `client.Beta.Sessions` |
| `src/LessonC09.Web/Live/ResourceSetup.cs` | `scripts/setup-resources.mts` | `dotnet run -- setup` provisioning |
| `src/LessonC09.Web/Components/Pages/Home.razor` + `LabWorkbench.razor` | `public/index.html` + `src/client.ts` | Lab page; the workbench is the interactive part |
| `src/LessonC09.Web/Program.cs` | `src/server.ts` | Hosting, `.env` loading, `/api/lesson`, `/api/run`, `/api/live` |
| `src/LessonC09.Web/wwwroot/` | `public/` | Guide, architecture page, styles, Prism (with an added C# grammar) |
| `tests/LessonC09.Tests/` | `src/lesson.test.ts` | xUnit scenario checks plus observer recovery tests |

### Differences from the Node version

- The lab UI calls `LabRunner` over the Blazor Server circuit instead of `fetch`. The JSON API is kept with the same origin, content-type, size and busy checks for scripts.
- Live commentary opens the event stream with `Events.WithRawResponse.StreamStreaming(...)` (which returns once the stream is connected) **before** sending `user.message`, as the original does. If the stream closes without an answer, it reads persisted history with `Events.List` and deduplicates by event ID. That applies the lesson's recovery rule to a real session. It never re-sends the prompt or creates another session.

## Check your understanding

Run **Recover overlapping delivery** and expect `passed` with `{"duplicates": 1, "delivered": 3}`. Then run **Deduplicate repeated live delivery** (`passed`, `{"duplicates": 2, "delivered": 1}`) and identify which evidence field explains the difference.

## Validation

```powershell
dotnet test
```
