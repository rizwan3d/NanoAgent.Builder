namespace NanoAgent.Builder.Infrastructure.Workspaces;

public sealed class ProjectWorkspaceOptions
{
    public const string SectionName = "ProjectWorkspaces";
    public const string DefaultRootPath = "App_Data/Workspaces";

    public string RootPath { get; set; } = DefaultRootPath;
}
