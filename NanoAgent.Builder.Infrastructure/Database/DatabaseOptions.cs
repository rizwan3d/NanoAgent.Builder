namespace NanoAgent.Builder.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; init; } = SupportedDatabaseProviders.Sqlite;

    public bool EnsureCreated { get; init; } = true;
}
