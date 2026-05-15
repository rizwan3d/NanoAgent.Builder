namespace NanoAgent.Builder.Application.Projects;

public interface IAgentProjectService
{
    Task<IReadOnlyList<AgentProjectDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<AgentProjectDto> CreateAsync(CreateAgentProjectRequest request, CancellationToken cancellationToken = default);

    Task<AgentProjectDto> RenameAsync(RenameAgentProjectRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default);
}
