namespace NanoAgent.Builder.Application.Abstractions;

public interface IProjectWorkspaceSetupQueue
{
    ValueTask QueueAsync(Guid projectId, CancellationToken cancellationToken = default);
}
