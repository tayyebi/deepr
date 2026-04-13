using System.Collections.Concurrent;
using Deepr.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Deepr.Infrastructure.AgentDrivers;

/// <summary>
/// Singleton registry that holds configured AI providers and lazily creates
/// IChatCompletionService instances keyed by (providerName, modelId).
/// </summary>
public class AiProviderRegistry
{
    private readonly Dictionary<string, AiProviderConfig> _configs;
    private readonly ConcurrentDictionary<(string Provider, string Model), IChatCompletionService> _services = new();
    private readonly ILogger _logger;

    public AiProviderRegistry(IReadOnlyList<AiProviderConfig> providers, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AiProviderRegistry>();
        _configs = new Dictionary<string, AiProviderConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in providers)
        {
            if (string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.Type))
            {
                _logger.LogWarning("Skipping AI provider with missing Name or Type");
                continue;
            }
            _configs[p.Name] = p;
            _logger.LogInformation("Registered AI provider '{Name}' (type={Type}, model={Model})", p.Name, p.Type, p.DefaultModel);
        }

        DefaultProviderName = _configs.Keys.FirstOrDefault();
    }

    public string? DefaultProviderName { get; }

    public IReadOnlyList<string> GetProviderNames() => _configs.Keys.ToList();

    public AiProviderConfig? GetProviderConfig(string name)
        => _configs.TryGetValue(name, out var cfg) ? cfg : null;

    public IChatCompletionService? GetService(string providerName, string? modelOverride = null)
    {
        if (!_configs.TryGetValue(providerName, out var config))
        {
            _logger.LogWarning("AI provider '{Provider}' not found in configuration", providerName);
            return null;
        }

        var model = string.IsNullOrWhiteSpace(modelOverride) ? config.DefaultModel : modelOverride;
        var key = (providerName, model);

        return _services.GetOrAdd(key, _ => CreateService(config, model));
    }

    private IChatCompletionService CreateService(AiProviderConfig config, string model)
    {
        var type = config.Type.ToLowerInvariant();
        _logger.LogInformation("Creating chat completion service for provider '{Name}' with model '{Model}'", config.Name, model);

        var builder = Kernel.CreateBuilder();

        switch (type)
        {
            case "openai":
                builder.AddOpenAIChatCompletion(
                    modelId: model,
                    apiKey: config.ApiKey ?? throw new InvalidOperationException($"ApiKey required for OpenAI provider '{config.Name}'"));
                break;

            case "ollama":
            case "openai-compatible":
                var endpoint = config.Endpoint ?? throw new InvalidOperationException($"Endpoint required for {type} provider '{config.Name}'");
                builder.AddOpenAIChatCompletion(
                    modelId: model,
                    apiKey: config.ApiKey ?? "not-required",
                    endpoint: new Uri(endpoint));
                break;

            case "azure-openai":
                var azureEndpoint = config.Endpoint ?? throw new InvalidOperationException($"Endpoint required for Azure OpenAI provider '{config.Name}'");
                var deploymentName = config.DeploymentName ?? model;
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: deploymentName,
                    endpoint: azureEndpoint,
                    apiKey: config.ApiKey ?? throw new InvalidOperationException($"ApiKey required for Azure OpenAI provider '{config.Name}'"));
                break;

            default:
                throw new InvalidOperationException($"Unknown AI provider type '{type}' for provider '{config.Name}'");
        }

        var kernel = builder.Build();
        return kernel.GetRequiredService<IChatCompletionService>();
    }
}
