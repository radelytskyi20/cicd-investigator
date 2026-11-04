if (args.Length == 4 && args[0] == "--copilot-lifecycle-smoke")
{
    await CiCdInvestigator.Api.CopilotLifecycleSmoke.RunAsync(args[1], args[2], args[3]);
    return;
}

if (args.Length == 4 && args[0] == "--copilot-smoke")
{
    await CiCdInvestigator.Api.CopilotSmoke.RunAsync(args[1], args[2], args[3]);
    return;
}

var app = CiCdInvestigator.Api.ApiApplication.Build(args);
await app.RunAsync();
