using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Infrastructure.Data;
using NanoAgent.Builder.Infrastructure.Data.Repositories;
using NanoAgent.Builder.Infrastructure.Database;

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

        services.AddDbContext<BuilderDbContext>(options =>
        {
            if (provider == SupportedDatabaseProviders.PostgreSql)
            {
                options.UseNpgsql(connectionString);
                return;
            }

            options.UseSqlite(connectionString);
        });

        services.AddScoped<IAgentProjectRepository, EfAgentProjectRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IDatabaseInfoProvider>(
            new ConfiguredDatabaseInfoProvider(new DatabaseInfo(provider, connectionStringName)));

        return services;
    }
}
