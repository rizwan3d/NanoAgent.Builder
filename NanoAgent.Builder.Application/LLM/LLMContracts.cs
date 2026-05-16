using NanoAgent.Builder.Application.Workspace;

namespace NanoAgent.Builder.Application.LLM;

public sealed record LLMGenerationRequest(
    Guid ProjectId,
    string ProjectName,
    string? ProjectDescription,
    string RequestedModel,
    string UserMessage,
    string WorkspaceRootPath,
    IReadOnlyList<ProjectFileDto> Files);

public abstract record LLMStreamEvent;

public sealed record LLMTextDelta(string Text) : LLMStreamEvent;

public sealed record LLMToolDelta(string Title, string Status, string? Detail) : LLMStreamEvent;

public sealed record LLMFilePatchDelta(GeneratedFilePatch Patch) : LLMStreamEvent;

public sealed record LLMUsageDelta(int InputTokens, int OutputTokens) : LLMStreamEvent;

public sealed record LLMGenerationCompleted(IReadOnlyList<GeneratedFilePatch> Patches, int InputTokens, int OutputTokens) : LLMStreamEvent;

public sealed record GeneratedFilePatch(
    string Path,
    string Language,
    string Content,
    string ChangeKind);
