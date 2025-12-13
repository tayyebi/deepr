using Deepr.Domain.Enums;

namespace Deepr.Application.Interfaces;

public interface IToolAdapter
{
    ToolType ToolType { get; }
    Task<object> AdaptAsync(object input, CancellationToken cancellationToken = default);
}
