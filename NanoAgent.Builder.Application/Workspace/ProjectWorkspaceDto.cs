using NanoAgent.Builder.Application.Projects;
using NanoAgent.Builder.Application.Saas;

namespace NanoAgent.Builder.Application.Workspace;

public sealed record ProjectWorkspaceDto(
    AgentProjectDto Project,
    IReadOnlyList<ProjectFileDto> Files,
    IReadOnlyList<ProjectMessageDto> Messages,
    IReadOnlyList<ProjectRunDto> Runs,
    IReadOnlyList<GeneratedArtifactDto> Artifacts,
    TokenUsageDto TokenUsage,
    IReadOnlyList<string> AllowedModels,
    Guid? SelectedFileId);
