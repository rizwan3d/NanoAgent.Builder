namespace NanoAgent.Builder.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; init; } = SupportedDatabaseProviders.Sqlite;

    public bool ApplyMigrations { get; init; } = true;

    // EF Core 9+ throws during MigrateAsync when the runtime model differs from the
    // migration snapshot. This project supports PostgreSQL and SQLite from one codebase,
    // so provider-specific annotations can otherwise trigger PendingModelChangesWarning
    // until provider-specific migrations are regenerated locally.
    public bool SuppressPendingModelChangesWarning { get; init; } = true;
}
