using Deepr.Domain.Common;

namespace Deepr.Domain.Entities;

public class SessionRound : BaseEntity
{
    public Guid SessionId { get; private set; }
    public int RoundNumber { get; private set; }
    public string Instructions { get; private set; }
    public string? Summary { get; private set; }

    private readonly List<Contribution> _contributions = new();
    public IReadOnlyCollection<Contribution> Contributions => _contributions.AsReadOnly();

    private SessionRound()
    {
        Instructions = string.Empty;
    }

    public SessionRound(Guid sessionId, int roundNumber, string instructions)
    {
        SessionId = sessionId;
        RoundNumber = roundNumber;
        Instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
    }

    public void AddContribution(Contribution contribution)
    {
        if (contribution == null)
            throw new ArgumentNullException(nameof(contribution));

        _contributions.Add(contribution);
    }

    public void SetSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Summary cannot be empty", nameof(summary));

        Summary = summary;
    }

    public void UpdateInstructions(string newInstructions)
    {
        if (string.IsNullOrWhiteSpace(newInstructions))
            throw new ArgumentException("Instructions cannot be empty", nameof(newInstructions));

        Instructions = newInstructions;
    }
}
