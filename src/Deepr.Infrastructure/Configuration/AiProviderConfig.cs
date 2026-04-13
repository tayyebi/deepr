namespace Deepr.Infrastructure.Configuration;

public class AiProviderConfig
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Provider type: "openai", "ollama", "azure-openai", "openai-compatible"</summary>
    public string Type { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string DefaultModel { get; set; } = string.Empty;
    /// <summary>Azure OpenAI deployment name (azure-openai type only)</summary>
    public string? DeploymentName { get; set; }
    public int TimeoutSeconds { get; set; } = 120;
}
