using Deepr.Domain.Common;

namespace Deepr.Domain.Entities;

public class Contribution : BaseEntity
{
    public Guid SessionRoundId { get; private set; }
    public Guid AgentId { get; private set; }
    public string RawContent { get; private set; }
    public string StructuredData { get; private set; }

    private Contribution()
    {
        RawContent = string.Empty;
        StructuredData = string.Empty;
    }

    public Contribution(Guid sessionRoundId, Guid agentId, string rawContent, string structuredData)
    {
        SessionRoundId = sessionRoundId;
        AgentId = agentId;
        RawContent = rawContent ?? throw new ArgumentNullException(nameof(rawContent));
        StructuredData = structuredData ?? throw new ArgumentNullException(nameof(structuredData));
    }

    public void UpdateStructuredData(string newStructuredData)
    {
        if (string.IsNullOrWhiteSpace(newStructuredData))
            throw new ArgumentException("Structured data cannot be empty", nameof(newStructuredData));

        StructuredData = newStructuredData;
    }
}
