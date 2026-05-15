namespace NanoAgent.Builder.Infrastructure.Database;

internal static class SqliteConnectionStringHelper
{
    public static string ResolveDatabasePath(string connectionString, string contentRootPath)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hasDataSource = false;

        for (var index = 0; index < parts.Length; index++)
        {
            var keyValue = parts[index].Split('=', 2, StringSplitOptions.TrimEntries);
            if (keyValue.Length != 2 || !IsDataSourceKey(keyValue[0]))
            {
                continue;
            }

            hasDataSource = true;
            var dataSource = keyValue[1];

            if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var resolvedDataSource = Path.IsPathRooted(dataSource)
                ? dataSource
                : Path.GetFullPath(Path.Combine(contentRootPath, dataSource));

            var directory = Path.GetDirectoryName(resolvedDataSource);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            parts[index] = $"{keyValue[0]}={resolvedDataSource}";
        }

        if (!hasDataSource)
        {
            throw new InvalidOperationException("SQLite connection string must include a Data Source value.");
        }

        return string.Join(';', parts);
    }

    private static bool IsDataSourceKey(string key) =>
        key.Equals("Data Source", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("DataSource", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("Filename", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("File Name", StringComparison.OrdinalIgnoreCase);
}
