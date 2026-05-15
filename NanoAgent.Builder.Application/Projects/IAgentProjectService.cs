namespace NanoAgent.Builder.Application.Projects;

public interface IAgentProjectService
{
    Task<IReadOnlyList<AgentProjectDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<AgentProjectDto> CreateAsync(CreateAgentProjectRequest request, CancellationToken cancellationToken = default);
}
