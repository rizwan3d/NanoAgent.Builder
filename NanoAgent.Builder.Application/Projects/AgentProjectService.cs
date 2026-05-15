using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Projects;

internal sealed class AgentProjectService : IAgentProjectService
{
    private readonly IAgentProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;

    public AgentProjectService(IAgentProjectRepository projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AgentProjectDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projects.ListAsync(cancellationToken);
        return projects.Select(MapToDto).ToList();
    }

    public async Task<AgentProjectDto> CreateAsync(CreateAgentProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = new AgentProject(request.Name, request.Description);

        await _projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    private static AgentProjectDto MapToDto(AgentProject project) =>
        new(project.Id, project.Name, project.Description, project.CreatedAtUtc);
}
