using Microsoft.EntityFrameworkCore;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Infrastructure.Data.Repositories;

internal sealed class EfAgentProjectRepository : IAgentProjectRepository
{
    private readonly BuilderDbContext _context;

    public EfAgentProjectRepository(BuilderDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AgentProject>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.AgentProjects
            .AsNoTracking()
            .OrderByDescending(project => project.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AgentProject>> ListForOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
        await _context.AgentProjects
            .AsNoTracking()
            .Where(project => project.OwnerUserId == ownerUserId)
            .OrderByDescending(project => project.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<int> CountForOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
        _context.AgentProjects.CountAsync(project => project.OwnerUserId == ownerUserId, cancellationToken);

    public async Task AddAsync(AgentProject project, CancellationToken cancellationToken = default) =>
        await _context.AgentProjects.AddAsync(project, cancellationToken);
}
