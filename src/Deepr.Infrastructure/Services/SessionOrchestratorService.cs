using Deepr.Application.Interfaces;
using Deepr.Domain.Entities;
using Deepr.Domain.Enums;

namespace Deepr.Infrastructure.Services;

public class SessionOrchestratorService : ISessionOrchestrator
{
    private readonly IEnumerable<IDecisionMethod> _decisionMethods;
    private readonly IEnumerable<IToolAdapter> _toolAdapters;
    private readonly IAgentDriver _agentDriver;

    public SessionOrchestratorService(
        IEnumerable<IDecisionMethod> decisionMethods,
        IEnumerable<IToolAdapter> toolAdapters,
        IAgentDriver agentDriver)
    {
        _decisionMethods = decisionMethods;
        _toolAdapters = toolAdapters;
        _agentDriver = agentDriver;
    }

    public async Task<Session> StartSessionAsync(Council council, Issue issue, CancellationToken cancellationToken = default)
    {
        var method = GetDecisionMethod(council.SelectedMethod);

        if (!await method.ValidateCouncilAsync(council, cancellationToken))
            throw new InvalidOperationException($"Council configuration is not valid for method {council.SelectedMethod}");

        var initialState = await method.InitializeStateAsync(council, issue, cancellationToken);
        return new Session(council.Id, initialState);
    }

    public async Task<SessionRound> ExecuteNextRoundAsync(Session session, Council council, CancellationToken cancellationToken = default)
    {
        var method = GetDecisionMethod(council.SelectedMethod);
        var toolAdapter = GetToolAdapter(council.SelectedTool);

        var promptResult = await method.GetNextPromptAsync(session, cancellationToken);

        if (promptResult.IsSessionComplete)
        {
            session.Complete();
            throw new InvalidOperationException("Session is already complete. Finalize instead.");
        }

        var toolTemplate = await toolAdapter.GeneratePromptTemplateAsync(cancellationToken);
        var fullPrompt = $"{promptResult.PromptText}\n\n{toolTemplate}";

        var roundNumber = session.CurrentRoundNumber + 1;
        var round = new SessionRound(session.Id, roundNumber, fullPrompt);

        foreach (var agent in council.Agents)
        {
            if (!await _agentDriver.IsAgentAvailableAsync(agent.AgentId, cancellationToken))
                continue;

            var rawResponse = await _agentDriver.GetResponseAsync(agent, fullPrompt, cancellationToken);
            var parsed = await toolAdapter.ParseResponseAsync(rawResponse, cancellationToken);

            var contribution = new Contribution(
                round.Id,
                agent.AgentId,
                rawResponse,
                parsed.StructuredDataJson);

            round.AddContribution(contribution);
        }

        var aggregation = await method.AggregateRoundAsync(round, session.StatePayload, cancellationToken);
        round.SetSummary(aggregation.SummaryText);
        session.UpdateStatePayload(aggregation.UpdatedStatePayload);
        session.AddRound(round);

        if (!aggregation.ShouldContinue)
        {
            session.Complete();
        }

        return round;
    }

    public async Task<bool> ShouldContinueSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(session.Status == SessionStatus.Active);
    }

    public async Task<string> FinalizeSessionAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session.Status != SessionStatus.Completed)
            session.Complete();

        // Use the StatePayload for the final result since rounds may not be eagerly loaded
        var statePayload = session.StatePayload;

        var summaries = session.Rounds
            .Where(r => r.Summary != null)
            .Select(r => $"Round {r.RoundNumber}: {r.Summary}")
            .ToList();

        if (summaries.Any())
            return await Task.FromResult(string.Join("\n\n", summaries));

        // Fallback: return a formatted summary from state payload
        return await Task.FromResult(
            $"Session completed.\n\nFinal state:\n{statePayload}");
    }

    private IDecisionMethod GetDecisionMethod(MethodType type)
    {
        return _decisionMethods.FirstOrDefault(m => m.Type == type)
            ?? throw new InvalidOperationException($"No decision method implementation found for type {type}");
    }

    private IToolAdapter GetToolAdapter(ToolType type)
    {
        return _toolAdapters.FirstOrDefault(a => a.Type == type)
            ?? throw new InvalidOperationException($"No tool adapter implementation found for type {type}");
    }
}
