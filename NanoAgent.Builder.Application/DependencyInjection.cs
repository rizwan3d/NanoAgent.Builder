using Microsoft.Extensions.DependencyInjection;
using NanoAgent.Builder.Application.Admin;
using NanoAgent.Builder.Application.Projects;
using NanoAgent.Builder.Application.Saas;

namespace NanoAgent.Builder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentProjectService, AgentProjectService>();
        services.AddScoped<ISaasSubscriptionService, SaasSubscriptionService>();
        services.AddScoped<IBillingCheckoutService, BillingCheckoutService>();
        services.AddScoped<ISubscriptionProvisioningService, SubscriptionProvisioningService>();
        services.AddScoped<IProjectQuotaService, ProjectQuotaService>();
        services.AddScoped<ITokenUsageService, TokenUsageService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        return services;
    }
}
