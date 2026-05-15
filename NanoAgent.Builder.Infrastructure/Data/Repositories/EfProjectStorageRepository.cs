using Microsoft.EntityFrameworkCore;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Infrastructure.Data.Repositories;

internal sealed class EfProjectStorageRepository : IProjectStorageRepository
{
    private readonly BuilderDbContext _context;

    public EfProjectStorageRepository(BuilderDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProjectFile>> ListFilesAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await _context.ProjectFiles
            .AsNoTracking()
            .Where(file => file.ProjectId == projectId)
            .OrderBy(file => file.Path)
            .ToListAsync(cancellationToken);

    public async Task<ProjectFile?> GetFileAsync(Guid projectId, Guid fileId, CancellationToken cancellationToken = default) =>
        await _context.ProjectFiles
            .FirstOrDefaultAsync(file => file.ProjectId == projectId && file.Id == fileId, cancellationToken);

    public async Task<IReadOnlyList<ProjectMessage>> ListMessagesAsync(Guid projectId, int take = 50, CancellationToken cancellationToken = default) =>
        await _context.ProjectMessages
            .AsNoTracking()
            .Where(message => message.ProjectId == projectId)
            .OrderByDescending(message => message.CreatedAtUtc)
            .Take(take)
            .OrderBy(message => message.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectRun>> ListRunsAsync(Guid projectId, int take = 25, CancellationToken cancellationToken = default) =>
        await _context.ProjectRuns
            .AsNoTracking()
            .Where(run => run.ProjectId == projectId)
            .OrderByDescending(run => run.StartedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GeneratedArtifact>> ListArtifactsAsync(Guid projectId, int take = 25, CancellationToken cancellationToken = default) =>
        await _context.GeneratedArtifacts
            .AsNoTracking()
            .Where(artifact => artifact.ProjectId == projectId)
            .OrderByDescending(artifact => artifact.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task AddFileAsync(ProjectFile file, CancellationToken cancellationToken = default) =>
        await _context.ProjectFiles.AddAsync(file, cancellationToken);

    public async Task AddMessageAsync(ProjectMessage message, CancellationToken cancellationToken = default) =>
        await _context.ProjectMessages.AddAsync(message, cancellationToken);

    public async Task AddRunAsync(ProjectRun run, CancellationToken cancellationToken = default) =>
        await _context.ProjectRuns.AddAsync(run, cancellationToken);

    public async Task AddArtifactAsync(GeneratedArtifact artifact, CancellationToken cancellationToken = default) =>
        await _context.GeneratedArtifacts.AddAsync(artifact, cancellationToken);
}
