using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Abstractions;

public interface IAgentProjectRepository
{
    Task<IReadOnlyList<AgentProject>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AgentProject project, CancellationToken cancellationToken = default);
}
