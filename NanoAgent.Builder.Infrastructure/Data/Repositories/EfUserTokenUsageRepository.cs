using Microsoft.EntityFrameworkCore;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Infrastructure.Data.Repositories;

internal sealed class EfUserTokenUsageRepository : IUserTokenUsageRepository
{
    private readonly BuilderDbContext _context;

    public EfUserTokenUsageRepository(BuilderDbContext context)
    {
        _context = context;
    }

    public async Task<MonthlyTokenUsage?> GetForUserPeriodAsync(
        string userId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default) =>
        await _context.MonthlyTokenUsages
            .FirstOrDefaultAsync(
                usage => usage.UserId == userId &&
                         usage.PeriodStartUtc == periodStartUtc &&
                         usage.PeriodEndUtc == periodEndUtc,
                cancellationToken);

    public async Task<IReadOnlyList<MonthlyTokenUsage>> ListForOpenPeriodsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default) =>
        await _context.MonthlyTokenUsages
            .AsNoTracking()
            .Where(usage => usage.PeriodStartUtc <= nowUtc && usage.PeriodEndUtc > nowUtc)
            .OrderByDescending(usage => usage.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(MonthlyTokenUsage usage, CancellationToken cancellationToken = default) =>
        await _context.MonthlyTokenUsages.AddAsync(usage, cancellationToken);
}
