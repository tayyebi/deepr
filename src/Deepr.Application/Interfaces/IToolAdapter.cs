using Deepr.Application.DTOs;
using Deepr.Domain.Enums;

namespace Deepr.Application.Interfaces;

public interface IToolAdapter
{
    ToolType Type { get; }

    /// <summary>
    /// Gets the JSON schema that defines the structure of data this tool expects.
    /// </summary>
    Task<ToolSchema> GetSchemaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses raw agent response text into structured data matching the tool's schema.
    /// </summary>
    Task<ParsedToolData> ParseResponseAsync(string rawContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the structured data conforms to the tool's schema.
    /// </summary>
    Task<bool> ValidateDataAsync(string structuredDataJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a prompt template that guides agents on how to structure their response for this tool.
    /// </summary>
    Task<string> GeneratePromptTemplateAsync(CancellationToken cancellationToken = default);
}
