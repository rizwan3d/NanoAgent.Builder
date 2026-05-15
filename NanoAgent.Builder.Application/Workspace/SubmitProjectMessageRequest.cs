namespace NanoAgent.Builder.Application.Workspace;

public sealed record SubmitProjectMessageRequest(
    Guid ProjectId,
    string Message,
    string LlmModel);
