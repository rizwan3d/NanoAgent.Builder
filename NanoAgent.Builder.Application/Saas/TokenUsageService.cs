using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Saas;

internal sealed class TokenUsageService : ITokenUsageService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly ISaasPlanRepository _plans;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IUserTokenUsageRepository _tokenUsage;
    private readonly IUnitOfWork _unitOfWork;

    public TokenUsageService(
        ICurrentUserContext currentUser,
        ISaasPlanRepository plans,
        IUserSubscriptionRepository subscriptions,
        IUserTokenUsageRepository tokenUsage,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _plans = plans;
        _subscriptions = subscriptions;
        _tokenUsage = tokenUsage;
        _unitOfWork = unitOfWork;
    }

    public async Task<TokenUsageDto?> GetCurrentUsageForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return null;
        }

        return await GetCurrentUsageForUserAsync(_currentUser.UserId, cancellationToken);
    }

    public async Task<TokenUsageDto> GetCurrentUsageForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entitlement = await GetEffectiveEntitlementAsync(userId, cancellationToken);
        var usage = await _tokenUsage.GetForUserPeriodAsync(userId, entitlement.PeriodStartUtc, entitlement.PeriodEndUtc, cancellationToken);

        return new TokenUsageDto(
            userId,
            entitlement.Plan.Code,
            entitlement.Plan.Name,
            entitlement.Plan.MonthlyTokenLimit,
            usage?.UsedTokens ?? 0,
            entitlement.PeriodStartUtc,
            entitlement.PeriodEndUtc,
            entitlement.Plan.GetAllowedLlmModels());
    }

    public async Task EnsureModelAllowedAsync(string userId, string llmModel, CancellationToken cancellationToken = default)
    {
        var entitlement = await GetEffectiveEntitlementAsync(userId, cancellationToken);
        EnsureModelAllowed(entitlement.Plan, llmModel);
    }

    public async Task EnsureCanUseTokensAsync(
        string userId,
        string llmModel,
        int requestedTokens,
        CancellationToken cancellationToken = default)
    {
        if (requestedTokens <= 0)
        {
            throw new DomainException("Requested token count must be greater than zero.");
        }

        var entitlement = await GetEffectiveEntitlementAsync(userId, cancellationToken);
        EnsureModelAllowed(entitlement.Plan, llmModel);

        if (entitlement.Plan.MonthlyTokenLimit == -1)
        {
            return;
        }

        var usage = await _tokenUsage.GetForUserPeriodAsync(userId, entitlement.PeriodStartUtc, entitlement.PeriodEndUtc, cancellationToken);
        var usedTokens = usage?.UsedTokens ?? 0;
        var remainingTokens = entitlement.Plan.MonthlyTokenLimit - usedTokens;

        if (requestedTokens > remainingTokens)
        {
            throw new DomainException(
                $"Your {entitlement.Plan.Name} package has {Math.Max(0, remainingTokens):N0} token(s) remaining this month. Upgrade your package or wait for the next monthly reset.");
        }
    }

    public async Task<TokenUsageDto> RecordUsageAsync(
        string userId,
        string llmModel,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken = default)
    {
        if (inputTokens < 0 || outputTokens < 0)
        {
            throw new DomainException("Input and output token counts cannot be negative.");
        }

        var totalTokens = inputTokens + outputTokens;
        await EnsureCanUseTokensAsync(userId, llmModel, totalTokens, cancellationToken);

        var entitlement = await GetEffectiveEntitlementAsync(userId, cancellationToken);
        var usage = await _tokenUsage.GetForUserPeriodAsync(userId, entitlement.PeriodStartUtc, entitlement.PeriodEndUtc, cancellationToken);
        if (usage is null)
        {
            usage = new MonthlyTokenUsage(userId, entitlement.PeriodStartUtc, entitlement.PeriodEndUtc);
            await _tokenUsage.AddAsync(usage, cancellationToken);
        }

        usage.AddTokens(totalTokens);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenUsageDto(
            userId,
            entitlement.Plan.Code,
            entitlement.Plan.Name,
            entitlement.Plan.MonthlyTokenLimit,
            usage.UsedTokens,
            entitlement.PeriodStartUtc,
            entitlement.PeriodEndUtc,
            entitlement.Plan.GetAllowedLlmModels());
    }

    private async Task<EffectiveEntitlement> GetEffectiveEntitlementAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User id is required for token entitlement checks.");
        }

        var subscription = await _subscriptions.GetCurrentForUserAsync(userId, cancellationToken);
        var plan = subscription?.Plan;

        if (plan is null)
        {
            plan = await _plans.GetByCodeAsync(SaasPlanCodes.Free, cancellationToken);
        }

        if (plan is null || !plan.IsActive)
        {
            throw new DomainException("No active SaaS package is configured.");
        }

        var (periodStartUtc, periodEndUtc) = ResolveMonthlyPeriod(subscription);
        return new EffectiveEntitlement(plan, periodStartUtc, periodEndUtc);
    }

    private static void EnsureModelAllowed(SubscriptionPlan plan, string llmModel)
    {
        if (string.IsNullOrWhiteSpace(llmModel))
        {
            throw new DomainException("LLM model is required.");
        }

        if (!plan.AllowsLlmModel(llmModel))
        {
            var allowedModels = string.Join(", ", plan.GetAllowedLlmModels());
            throw new DomainException(
                $"The {plan.Name} package does not allow the {llmModel} model. Allowed model(s): {allowedModels}.");
        }
    }

    private static (DateTimeOffset PeriodStartUtc, DateTimeOffset PeriodEndUtc) ResolveMonthlyPeriod(UserSubscription? subscription)
    {
        if (subscription?.CurrentPeriodStartsAtUtc is not null && subscription.CurrentPeriodEndsAtUtc is not null)
        {
            return (subscription.CurrentPeriodStartsAtUtc.Value, subscription.CurrentPeriodEndsAtUtc.Value);
        }

        if (subscription?.CurrentPeriodEndsAtUtc is not null)
        {
            return (subscription.CurrentPeriodEndsAtUtc.Value.AddMonths(-1), subscription.CurrentPeriodEndsAtUtc.Value);
        }

        var now = DateTimeOffset.UtcNow;
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return (start, start.AddMonths(1));
    }

    private sealed record EffectiveEntitlement(
        SubscriptionPlan Plan,
        DateTimeOffset PeriodStartUtc,
        DateTimeOffset PeriodEndUtc);
}
