namespace Deepr.Application.DTOs;

public class IssueDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContextVector { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
