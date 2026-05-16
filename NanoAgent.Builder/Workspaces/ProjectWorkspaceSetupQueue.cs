using System.Collections.Concurrent;
using NanoAgent.Builder.Application.Abstractions;

namespace NanoAgent.Builder.Workspaces;

internal sealed class ProjectWorkspaceSetupQueue : IProjectWorkspaceSetupQueue
{
    private readonly ConcurrentQueue<Guid> _items = new();
    private readonly SemaphoreSlim _signal = new(0);

    public ValueTask QueueAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
        {
            return ValueTask.CompletedTask;
        }

        _items.Enqueue(projectId);
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async Task<Guid> DequeueAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            await _signal.WaitAsync(cancellationToken);

            if (_items.TryDequeue(out var projectId))
            {
                return projectId;
            }
        }
    }
}
