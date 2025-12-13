using Deepr.Domain.Common;
using Deepr.Domain.Enums;

namespace Deepr.Domain.Entities;

public class Session : BaseEntity
{
    public Guid CouncilId { get; private set; }
    public SessionStatus Status { get; private set; }
    public int CurrentRoundNumber { get; private set; }
    public string StatePayload { get; private set; }

    private readonly List<SessionRound> _rounds = new();
    public IReadOnlyCollection<SessionRound> Rounds => _rounds.AsReadOnly();

    private Session()
    {
        StatePayload = "{}";
    }

    public Session(Guid councilId, string initialStatePayload = "{}")
    {
        CouncilId = councilId;
        Status = SessionStatus.Active;
        CurrentRoundNumber = 0;
        StatePayload = initialStatePayload ?? "{}";
    }

    public void AddRound(SessionRound round)
    {
        if (round == null)
            throw new ArgumentNullException(nameof(round));

        _rounds.Add(round);
        CurrentRoundNumber = round.RoundNumber;
    }

    public void UpdateStatePayload(string newStatePayload)
    {
        if (string.IsNullOrWhiteSpace(newStatePayload))
            throw new ArgumentException("State payload cannot be empty", nameof(newStatePayload));

        StatePayload = newStatePayload;
    }

    public void Pause()
    {
        if (Status == SessionStatus.Completed || Status == SessionStatus.Failed)
            throw new InvalidOperationException($"Cannot pause a session in {Status} state");

        Status = SessionStatus.Paused;
    }

    public void Resume()
    {
        if (Status != SessionStatus.Paused)
            throw new InvalidOperationException($"Cannot resume a session that is not paused");

        Status = SessionStatus.Active;
    }

    public void Complete()
    {
        if (Status == SessionStatus.Completed)
            throw new InvalidOperationException("Session is already completed");

        Status = SessionStatus.Completed;
    }

    public void Fail()
    {
        if (Status == SessionStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed session");

        Status = SessionStatus.Failed;
    }

    public SessionRound? GetCurrentRound()
    {
        return _rounds.FirstOrDefault(r => r.RoundNumber == CurrentRoundNumber);
    }

    public SessionRound? GetRound(int roundNumber)
    {
        return _rounds.FirstOrDefault(r => r.RoundNumber == roundNumber);
    }
}
