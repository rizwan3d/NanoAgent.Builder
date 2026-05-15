using Microsoft.Extensions.DependencyInjection;
using NanoAgent.Builder.Application.Projects;

namespace NanoAgent.Builder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentProjectService, AgentProjectService>();

        return services;
    }
}
