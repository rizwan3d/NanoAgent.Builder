using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Abstractions;

public interface IAgentProjectRepository
{
    Task<IReadOnlyList<AgentProject>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentProject>> ListForOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

    Task<AgentProject?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<int> CountForOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

    Task AddAsync(AgentProject project, CancellationToken cancellationToken = default);

    void Remove(AgentProject project);
}
