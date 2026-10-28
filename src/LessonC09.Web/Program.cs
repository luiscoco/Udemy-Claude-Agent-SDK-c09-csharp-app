using System.Text.Json.Nodes;
using LessonC09.Web;
using LessonC09.Web.Components;
using LessonC09.Web.Lessons;
using LessonC09.Web.Live;

var envFile = Path.Combine(AppPaths.Root, ".env");
DotEnv.Load(envFile);

if (args.FirstOrDefault() == "setup")
{
    await ResourceSetup.RunAsync(envFile);
    return;
}

var builder = WebApplication.CreateBuilder(args);
var port = int.TryParse(builder.Configuration["PORT"], out var configured) ? configured : 3000 + StreamRecoveryLesson.Number;
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
// Serve _framework/blazor.web.js from the build output in every environment, not only Development.
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<ManagedAgentCommentary>();
builder.Services.AddSingleton<LabRunner>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();

// Same JSON API as the original Node server, for scripts and parity checks.
var api = app.MapGroup("/api");
api.MapGet("/lesson", (LabRunner lab) => Results.Json(new
{
    number = StreamRecoveryLesson.Number,
    title = StreamRecoveryLesson.Title,
    description = StreamRecoveryLesson.Description,
    concepts = StreamRecoveryLesson.Concepts,
    scope = StreamRecoveryLesson.Scope,
    cases = StreamRecoveryLesson.Cases,
    liveConfigured = lab.LiveConfigured,
}, LabRunner.Json));
api.MapPost("/run", async (HttpRequest request, LabRunner lab) =>
    await Guarded(request, async input => Results.Json(await lab.RunLocalAsync(input, request.HttpContext.RequestAborted), LabRunner.Json)));
api.MapPost("/live", async (HttpRequest request, LabRunner lab) =>
    await Guarded(request, async input => Results.Json(await lab.RunLiveAsync(input, request.HttpContext.RequestAborted), LabRunner.Json)));

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStarted.Register(() =>
    Console.WriteLine($"Lesson C{StreamRecoveryLesson.Number:00} ready: http://127.0.0.1:{port}"));
app.Run();

async Task<IResult> Guarded(HttpRequest request, Func<JsonObject, Task<IResult>> run)
{
    var origin = request.Headers.Origin.ToString();
    if (origin.Length > 0 && origin != $"http://127.0.0.1:{port}" && origin != $"http://localhost:{port}")
        return Results.Json(new { error = "Foreign origin" }, statusCode: 403);
    if (!request.HasJsonContentType())
        return Results.Json(new { error = "JSON input required" }, statusCode: 415);
    if (request.ContentLength > 64000)
        return Results.Json(new { error = "Input exceeds the sample limit" }, statusCode: 413);
    try
    {
        var input = LessonInput.Object(await JsonNode.ParseAsync(request.Body));
        return await run(input);
    }
    catch (LabBusyException error) { return Results.Json(new { error = error.Message }, statusCode: 409); }
    catch (Exception error) when (error is LessonInputException or LiveObservationException or System.Text.Json.JsonException)
    {
        return Results.Json(new { error = error.Message }, statusCode: 400);
    }
}
