namespace Deepr.Application.DTOs;

public class ParsedToolData
{
    public string StructuredDataJson { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
}
