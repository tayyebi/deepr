namespace Deepr.Application.Interfaces;

public interface IAgentDriver
{
    Task<object> DriveAsync(object input, CancellationToken cancellationToken = default);
}
