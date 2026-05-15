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

    public async Task<IReadOnlyList<AgentProject>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.AgentProjects
            .AsNoTracking()
            .OrderByDescending(project => project.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AgentProject project, CancellationToken cancellationToken = default) =>
        await _context.AgentProjects.AddAsync(project, cancellationToken);
}
