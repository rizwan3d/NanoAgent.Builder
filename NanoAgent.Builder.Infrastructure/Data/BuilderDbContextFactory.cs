using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using NanoAgent.Builder.Infrastructure.Database;

namespace NanoAgent.Builder.Infrastructure.Data;

public sealed class BuilderDbContextFactory : IDesignTimeDbContextFactory<BuilderDbContext>
{
    public BuilderDbContext CreateDbContext(string[] args)
    {
        var contentRootPath = ResolveWebProjectPath(Directory.GetCurrentDirectory());
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(contentRootPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

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

        var optionsBuilder = new DbContextOptionsBuilder<BuilderDbContext>();
        if (provider == SupportedDatabaseProviders.PostgreSql)
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlite(connectionString);
        }

        if (databaseOptions.SuppressPendingModelChangesWarning)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        return new BuilderDbContext(optionsBuilder.Options);
    }

    private static string ResolveWebProjectPath(string currentDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(currentDirectory, "NanoAgent.Builder"),
            currentDirectory,
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "NanoAgent.Builder"))
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "appsettings.json")) &&
                File.Exists(Path.Combine(candidate, "NanoAgent.Builder.csproj")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Could not locate the NanoAgent.Builder web project for design-time EF Core migrations.");
    }
}
