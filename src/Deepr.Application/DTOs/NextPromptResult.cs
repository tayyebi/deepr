namespace Deepr.Application.DTOs;

public class NextPromptResult
{
    public string PromptText { get; set; } = string.Empty;
    public bool IsSessionComplete { get; set; }
    public string? CompletionReason { get; set; }
}
