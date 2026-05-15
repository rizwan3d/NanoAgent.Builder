using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Domain.Projects;

public sealed class AgentProject : Entity
{
    private AgentProject()
    {
    }

    public AgentProject(string ownerUserId, string name, string? description)
    {
        SetOwner(ownerUserId);
        Rename(name);
        UpdateDescription(description);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string OwnerUserId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Project name is required.");
        }

        if (name.Length > 200)
        {
            throw new DomainException("Project name cannot be longer than 200 characters.");
        }

        Name = name.Trim();
    }

    public void UpdateDescription(string? description)
    {
        if (description is { Length: > 1000 })
        {
            throw new DomainException("Project description cannot be longer than 1000 characters.");
        }

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private void SetOwner(string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new DomainException("A project must belong to a user.");
        }

        OwnerUserId = ownerUserId;
    }
}
