namespace NanoAgent.Builder.Application.Saas;

public interface ISaasSubscriptionService
{
    Task<IReadOnlyList<SaasPlanDto>> ListPlansAsync(CancellationToken cancellationToken = default);

    Task<UserSubscriptionDto?> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default);

    Task<UserSubscriptionDto> SubscribeCurrentUserAsync(string planCode, CancellationToken cancellationToken = default);

    Task<UserSubscriptionDto> SubscribeUserAsync(string userId, string planCode, CancellationToken cancellationToken = default);
}
