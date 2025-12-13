using Deepr.Domain.Enums;

namespace Deepr.Application.Interfaces;

public interface IDecisionMethod
{
    MethodType MethodType { get; }
    Task<object> ExecuteAsync(object input, CancellationToken cancellationToken = default);
}
