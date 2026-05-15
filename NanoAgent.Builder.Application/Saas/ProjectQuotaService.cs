using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Application.Saas;

internal sealed class ProjectQuotaService : IProjectQuotaService
{
    private readonly IAgentProjectRepository _projects;
    private readonly ISaasPlanRepository _plans;
    private readonly IUserSubscriptionRepository _subscriptions;

    public ProjectQuotaService(
        IAgentProjectRepository projects,
        ISaasPlanRepository plans,
        IUserSubscriptionRepository subscriptions)
    {
        _projects = projects;
        _plans = plans;
        _subscriptions = subscriptions;
    }

    public async Task EnsureCanCreateProjectAsync(string userId, CancellationToken cancellationToken = default)
    {
        var plan = await GetEffectivePlanAsync(userId, cancellationToken);

        if (plan.ProjectLimit == -1)
        {
            return;
        }

        var projectCount = await _projects.CountForOwnerAsync(userId, cancellationToken);
        if (projectCount >= plan.ProjectLimit)
        {
            throw new DomainException(
                $"Your current {plan.Name} plan allows up to {plan.ProjectLimit} project(s). Upgrade your package to create more projects.");
        }
    }

    private async Task<Domain.Saas.SubscriptionPlan> GetEffectivePlanAsync(string userId, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetCurrentForUserAsync(userId, cancellationToken);
        if (subscription?.Plan is not null)
        {
            return subscription.Plan;
        }

        var freePlan = await _plans.GetByCodeAsync(SaasPlanCodes.Free, cancellationToken);
        if (freePlan is not null)
        {
            return freePlan;
        }

        throw new DomainException("No active SaaS package is configured.");
    }
}
