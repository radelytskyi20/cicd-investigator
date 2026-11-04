using Microsoft.AspNetCore.SignalR;

namespace CiCdInvestigator.Api;

public sealed class InvestigationHub(InvestigationAgentService agent) : Hub
{
    public async Task Start(InvestigationRequest request) =>
        await agent.RunAsync(request, null, Context.ConnectionId, Context.ConnectionAborted);

    public async Task FollowUp(string message) =>
        await agent.RunAsync(null, message, Context.ConnectionId, Context.ConnectionAborted);

    public async Task Stop() => await agent.CancelAsync();

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await agent.CloseAsync();
        await base.OnDisconnectedAsync(exception);
    }
}
