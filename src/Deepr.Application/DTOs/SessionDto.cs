using Deepr.Domain.Enums;

namespace Deepr.Application.DTOs;

public class SessionDto
{
    public Guid Id { get; set; }
    public Guid CouncilId { get; set; }
    public SessionStatus Status { get; set; }
    public int CurrentRoundNumber { get; set; }
    public string StatePayload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
