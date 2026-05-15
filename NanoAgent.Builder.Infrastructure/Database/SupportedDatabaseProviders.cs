namespace NanoAgent.Builder.Infrastructure.Database;

internal static class SupportedDatabaseProviders
{
    public const string Sqlite = "Sqlite";
    public const string PostgreSql = "PostgreSql";

    public static string Normalize(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Sqlite;
        }

        return provider.Trim().ToLowerInvariant() switch
        {
            "sqlite" or "sql lite" or "sql-lite" => Sqlite,
            "postgresql" or "postgres" or "postgres sql" or "pgsql" => PostgreSql,
            _ => throw new InvalidOperationException(
                $"Unsupported database provider '{provider}'. Use '{Sqlite}' or '{PostgreSql}'.")
        };
    }

    public static string GetConnectionStringName(string provider) =>
        provider == PostgreSql ? "PostgreSqlConnection" : "SqliteConnection";
}
