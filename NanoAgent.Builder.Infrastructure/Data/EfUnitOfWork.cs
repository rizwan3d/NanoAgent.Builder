using NanoAgent.Builder.Application.Abstractions;

namespace NanoAgent.Builder.Infrastructure.Data;

internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly BuilderDbContext _context;

    public EfUnitOfWork(BuilderDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
