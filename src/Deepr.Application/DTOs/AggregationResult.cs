namespace Deepr.Application.DTOs;

public class AggregationResult
{
    public string SummaryText { get; set; } = string.Empty;
    public string UpdatedStatePayload { get; set; } = string.Empty;
    public bool ShouldContinue { get; set; }
}
