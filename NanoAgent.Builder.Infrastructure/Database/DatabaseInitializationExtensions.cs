using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NanoAgent.Builder.Infrastructure.Data;

namespace NanoAgent.Builder.Infrastructure.Database;

public static class DatabaseInitializationExtensions
{
    public static async Task InitialiseDatabaseAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        if (!databaseOptions.EnsureCreated)
        {
            return;
        }

        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BuilderDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
