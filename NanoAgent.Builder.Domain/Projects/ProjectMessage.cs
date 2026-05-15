using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Domain.Projects;

public sealed class ProjectMessage : Entity
{
    private ProjectMessage()
    {
    }

    public ProjectMessage(
        Guid projectId,
        string role,
        string content,
        string llmModel,
        int inputTokens,
        int outputTokens)
    {
        SetProject(projectId);
        SetRole(role);
        SetContent(content);
        SetLlmModel(llmModel);
        SetTokenCounts(inputTokens, outputTokens);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid ProjectId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public string LlmModel { get; private set; } = string.Empty;

    public int InputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public AgentProject? Project { get; private set; }

    private void SetProject(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new DomainException("A message must belong to a project.");
        }

        ProjectId = projectId;
    }

    private void SetRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainException("Message role is required.");
        }

        if (role.Length > 50)
        {
            throw new DomainException("Message role cannot be longer than 50 characters.");
        }

        Role = role.Trim().ToLowerInvariant();
    }

    private void SetContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Message content is required.");
        }

        Content = content.Trim();
    }

    private void SetLlmModel(string llmModel)
    {
        if (string.IsNullOrWhiteSpace(llmModel))
        {
            throw new DomainException("LLM model is required for a message.");
        }

        if (llmModel.Length > 100)
        {
            throw new DomainException("LLM model cannot be longer than 100 characters.");
        }

        LlmModel = llmModel.Trim();
    }

    private void SetTokenCounts(int inputTokens, int outputTokens)
    {
        if (inputTokens < 0 || outputTokens < 0)
        {
            throw new DomainException("Message token counts cannot be negative.");
        }

        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }
}
