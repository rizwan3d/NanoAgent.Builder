using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Projects;

internal sealed class AgentProjectService : IAgentProjectService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IAgentProjectRepository _projects;
    private readonly IProjectQuotaService _quotaService;
    private readonly IUnitOfWork _unitOfWork;

    public AgentProjectService(
        ICurrentUserContext currentUser,
        IAgentProjectRepository projects,
        IProjectQuotaService quotaService,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projects = projects;
        _quotaService = quotaService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AgentProjectDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        RequireSignedInUser();

        var projects = _currentUser.IsAdmin
            ? await _projects.ListAllAsync(cancellationToken)
            : await _projects.ListForOwnerAsync(_currentUser.UserId!, cancellationToken);

        return projects.Select(MapToDto).ToList();
    }

    public async Task<AgentProjectDto> CreateAsync(CreateAgentProjectRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireSignedInUser();

        await _quotaService.EnsureCanCreateProjectAsync(userId, cancellationToken);

        var project = new AgentProject(userId, request.Name, request.Description);

        await _projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    private string RequireSignedInUser()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new DomainException("You must sign in before managing projects.");
        }

        return _currentUser.UserId;
    }

    private static AgentProjectDto MapToDto(AgentProject project) =>
        new(project.Id, project.OwnerUserId, project.Name, project.Description, project.CreatedAtUtc);
}
