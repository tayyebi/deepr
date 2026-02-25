namespace Deepr.Application.DTOs;

public class DecisionSheetExport
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/markdown";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
