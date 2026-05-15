using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Domain.Projects;

public sealed class ProjectRun : Entity
{
    private ProjectRun()
    {
    }

    public ProjectRun(Guid projectId, string status, string requestedModel, string? prompt, int inputTokens, int outputTokens)
    {
        SetProject(projectId);
        SetStatus(status);
        SetRequestedModel(requestedModel);
        SetPrompt(prompt);
        SetTokenCounts(inputTokens, outputTokens);
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid ProjectId { get; private set; }

    public string Status { get; private set; } = string.Empty;

    public string RequestedModel { get; private set; } = string.Empty;

    public string? Prompt { get; private set; }

    public int InputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public string? ErrorMessage { get; private set; }

    public AgentProject? Project { get; private set; }

    public void Complete(int outputTokens)
    {
        if (outputTokens < 0)
        {
            throw new DomainException("Output token count cannot be negative.");
        }

        OutputTokens = outputTokens;
        Status = "completed";
        CompletedAtUtc = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    public void Fail(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new DomainException("Run error message is required.");
        }

        Status = "failed";
        ErrorMessage = errorMessage.Trim();
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    private void SetProject(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new DomainException("A run must belong to a project.");
        }

        ProjectId = projectId;
    }

    private void SetStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new DomainException("Run status is required.");
        }

        if (status.Length > 50)
        {
            throw new DomainException("Run status cannot be longer than 50 characters.");
        }

        Status = status.Trim().ToLowerInvariant();
    }

    private void SetRequestedModel(string requestedModel)
    {
        if (string.IsNullOrWhiteSpace(requestedModel))
        {
            throw new DomainException("Requested model is required.");
        }

        if (requestedModel.Length > 100)
        {
            throw new DomainException("Requested model cannot be longer than 100 characters.");
        }

        RequestedModel = requestedModel.Trim();
    }

    private void SetPrompt(string? prompt)
    {
        if (prompt is { Length: > 8000 })
        {
            throw new DomainException("Run prompt cannot be longer than 8000 characters.");
        }

        Prompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt.Trim();
    }

    private void SetTokenCounts(int inputTokens, int outputTokens)
    {
        if (inputTokens < 0 || outputTokens < 0)
        {
            throw new DomainException("Run token counts cannot be negative.");
        }

        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }
}
