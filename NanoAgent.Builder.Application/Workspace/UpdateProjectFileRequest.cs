namespace NanoAgent.Builder.Application.Workspace;

public sealed record UpdateProjectFileRequest(
    Guid ProjectId,
    Guid FileId,
    string Content);
