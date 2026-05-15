using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Infrastructure.Data;
using NanoAgent.Builder.Infrastructure.Data.Repositories;
using NanoAgent.Builder.Infrastructure.Database;
using NanoAgent.Builder.Infrastructure.Identity;
using NanoAgent.Builder.Infrastructure.Payments;
using NanoAgent.Builder.Infrastructure.Workspaces;

namespace NanoAgent.Builder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        var provider = SupportedDatabaseProviders.Normalize(databaseOptions.Provider);
        var connectionStringName = SupportedDatabaseProviders.GetConnectionStringName(provider);
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' is required for provider '{provider}'.");
        }

        if (provider == SupportedDatabaseProviders.Sqlite)
        {
            connectionString = SqliteConnectionStringHelper.ResolveDatabasePath(connectionString, contentRootPath);
        }

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<SeedAdminOptions>(configuration.GetSection(SeedAdminOptions.SectionName));
        services.Configure<ProjectWorkspaceOptions>(options =>
        {
            var configuredOptions = configuration
                .GetSection(ProjectWorkspaceOptions.SectionName)
                .Get<ProjectWorkspaceOptions>() ?? new ProjectWorkspaceOptions();

            var configuredRootPath = string.IsNullOrWhiteSpace(configuredOptions.RootPath)
                ? ProjectWorkspaceOptions.DefaultRootPath
                : configuredOptions.RootPath.Trim();

            options.RootPath = Path.IsPathRooted(configuredRootPath)
                ? Path.GetFullPath(configuredRootPath)
                : Path.GetFullPath(Path.Combine(contentRootPath, configuredRootPath));
        });
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));

        services.AddDbContext<BuilderDbContext>(options =>
        {
            if (provider == SupportedDatabaseProviders.PostgreSql)
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }

            if (databaseOptions.SuppressPendingModelChangesWarning)
            {
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddEntityFrameworkStores<BuilderDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IAgentProjectRepository, EfAgentProjectRepository>();
        services.AddScoped<ISaasPlanRepository, EfSaasPlanRepository>();
        services.AddScoped<IUserSubscriptionRepository, EfUserSubscriptionRepository>();
        services.AddScoped<IUserTokenUsageRepository, EfUserTokenUsageRepository>();
        services.AddScoped<IProjectStorageRepository, EfProjectStorageRepository>();
        services.AddScoped<IProjectWorkspaceFileSystem, ProjectWorkspaceFileSystem>();
        services.AddScoped<IApplicationUserReadRepository, IdentityUserReadRepository>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<IStripeWebhookHandler, StripeWebhookHandler>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IDatabaseInfoProvider>(
            new ConfiguredDatabaseInfoProvider(new DatabaseInfo(provider, connectionStringName)));

        return services;
    }
}
