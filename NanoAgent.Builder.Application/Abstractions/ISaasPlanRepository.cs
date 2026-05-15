using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Abstractions;

public interface ISaasPlanRepository
{
    Task<IReadOnlyList<SubscriptionPlan>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPlan>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);
}
