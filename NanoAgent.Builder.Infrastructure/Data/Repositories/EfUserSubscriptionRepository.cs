using Microsoft.EntityFrameworkCore;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Infrastructure.Data.Repositories;

internal sealed class EfUserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly BuilderDbContext _context;

    public EfUserSubscriptionRepository(BuilderDbContext context)
    {
        _context = context;
    }

    public Task<UserSubscription?> GetCurrentForUserAsync(string userId, CancellationToken cancellationToken = default) =>
        _context.UserSubscriptions
            .Include(subscription => subscription.Plan)
            .FirstOrDefaultAsync(subscription =>
                subscription.UserId == userId &&
                subscription.Status == SubscriptionStatus.Active &&
                subscription.EndsAtUtc == null,
                cancellationToken);

    public async Task<IReadOnlyList<UserSubscription>> ListCurrentAsync(CancellationToken cancellationToken = default) =>
        await _context.UserSubscriptions
            .AsNoTracking()
            .Include(subscription => subscription.Plan)
            .Where(subscription => subscription.Status == SubscriptionStatus.Active && subscription.EndsAtUtc == null)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default) =>
        await _context.UserSubscriptions.AddAsync(subscription, cancellationToken);
}
