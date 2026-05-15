namespace NanoAgent.Builder.Application.Abstractions;

public interface IDatabaseInfoProvider
{
    DatabaseInfo GetCurrent();
}

public sealed record DatabaseInfo(string Provider, string ConnectionStringName);
