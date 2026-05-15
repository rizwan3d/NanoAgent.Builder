using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Abstractions;

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetCurrentForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<UserSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSubscription>> ListCurrentAsync(CancellationToken cancellationToken = default);

    Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default);
}
