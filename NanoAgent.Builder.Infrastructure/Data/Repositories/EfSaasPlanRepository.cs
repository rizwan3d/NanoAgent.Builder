using Microsoft.EntityFrameworkCore;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Infrastructure.Data.Repositories;

internal sealed class EfSaasPlanRepository : ISaasPlanRepository
{
    private readonly BuilderDbContext _context;

    public EfSaasPlanRepository(BuilderDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SubscriptionPlan>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.DisplayOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SubscriptionPlan>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _context.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(plan => plan.DisplayOrder)
            .ToListAsync(cancellationToken);

    public Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.SubscriptionPlans
            .FirstOrDefaultAsync(plan => plan.Code == code.Trim().ToLowerInvariant(), cancellationToken);

    public Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.SubscriptionPlans.FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);

    public Task<SubscriptionPlan?> GetByStripePriceIdAsync(string stripePriceId, CancellationToken cancellationToken = default) =>
        _context.SubscriptionPlans
            .FirstOrDefaultAsync(plan => plan.StripePriceId == stripePriceId.Trim(), cancellationToken);

    public async Task AddAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default) =>
        await _context.SubscriptionPlans.AddAsync(plan, cancellationToken);
}
