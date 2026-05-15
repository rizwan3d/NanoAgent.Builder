using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Domain.Projects;

public sealed class ProjectFile : Entity
{
    private ProjectFile()
    {
    }

    public ProjectFile(Guid projectId, string path, string? language, string content)
    {
        SetProject(projectId);
        Rename(path);
        SetLanguage(language);
        UpdateContent(content);
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid ProjectId { get; private set; }

    public string Path { get; private set; } = string.Empty;

    public string? Language { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public AgentProject? Project { get; private set; }

    public void Rename(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new DomainException("File path is required.");
        }

        if (path.Length > 500)
        {
            throw new DomainException("File path cannot be longer than 500 characters.");
        }

        Path = path.Trim().Replace('\\', '/');
        Touch();
    }

    public void SetLanguage(string? language)
    {
        if (language is { Length: > 100 })
        {
            throw new DomainException("File language cannot be longer than 100 characters.");
        }

        Language = string.IsNullOrWhiteSpace(language) ? null : language.Trim();
        Touch();
    }

    public void UpdateContent(string content)
    {
        Content = content ?? string.Empty;
        Touch();
    }

    private void SetProject(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new DomainException("A file must belong to a project.");
        }

        ProjectId = projectId;
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
