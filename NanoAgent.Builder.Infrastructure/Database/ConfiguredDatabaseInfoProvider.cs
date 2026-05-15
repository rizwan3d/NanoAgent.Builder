using NanoAgent.Builder.Application.Abstractions;

namespace NanoAgent.Builder.Infrastructure.Database;

internal sealed class ConfiguredDatabaseInfoProvider : IDatabaseInfoProvider
{
    private readonly DatabaseInfo _databaseInfo;

    public ConfiguredDatabaseInfoProvider(DatabaseInfo databaseInfo)
    {
        _databaseInfo = databaseInfo;
    }

    public DatabaseInfo GetCurrent() => _databaseInfo;
}
