using Deepr.Infrastructure.AgentDrivers;
using Microsoft.AspNetCore.Mvc;

namespace Deepr.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly AiProviderRegistry _registry;

    public ProvidersController(AiProviderRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>Lists all configured AI providers</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProviderInfo>), StatusCodes.Status200OK)]
    public ActionResult<List<ProviderInfo>> GetProviders()
    {
        var providers = _registry.GetProviderNames().Select(name =>
        {
            var config = _registry.GetProviderConfig(name)!;
            return new ProviderInfo(name, config.Type, config.DefaultModel);
        }).ToList();

        return Ok(providers);
    }
}

public record ProviderInfo(string Name, string Type, string DefaultModel);
