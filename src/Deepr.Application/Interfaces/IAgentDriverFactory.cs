using Deepr.Domain.ValueObjects;

namespace Deepr.Application.Interfaces;

public interface IAgentDriverFactory
{
    IAgentDriver GetDriver(CouncilMember member);
}
