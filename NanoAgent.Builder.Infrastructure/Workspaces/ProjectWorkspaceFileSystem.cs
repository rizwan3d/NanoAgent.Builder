using System.Text;
using Microsoft.Extensions.Options;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Infrastructure.Workspaces;

internal sealed class ProjectWorkspaceFileSystem : IProjectWorkspaceFileSystem
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly string _workspaceRootPath;

    public ProjectWorkspaceFileSystem(IOptions<ProjectWorkspaceOptions> options)
    {
        _workspaceRootPath = options.Value.RootPath;
    }

    public string GetProjectRootPath(AgentProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var projectRootPath = Path.Combine(_workspaceRootPath, project.Id.ToString("N"));
        Directory.CreateDirectory(projectRootPath);
        return projectRootPath;
    }

    public async Task EnsureProjectWorkspaceAsync(
        AgentProject project,
        IReadOnlyList<ProjectFile> files,
        CancellationToken cancellationToken = default)
    {
        var projectRootPath = GetProjectRootPath(project);

        foreach (var file in files)
        {
            await WriteFileCoreAsync(projectRootPath, file, cancellationToken);
        }
    }

    public Task WriteFileAsync(
        AgentProject project,
        ProjectFile file,
        CancellationToken cancellationToken = default) =>
        WriteFileCoreAsync(GetProjectRootPath(project), file, cancellationToken);

    private static async Task WriteFileCoreAsync(
        string projectRootPath,
        ProjectFile file,
        CancellationToken cancellationToken)
    {
        var fullPath = ResolveProjectFilePath(projectRootPath, file.Path);
        var directoryPath = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(fullPath, file.Content ?? string.Empty, Utf8WithoutBom, cancellationToken);
    }

    private static string ResolveProjectFilePath(string projectRootPath, string relativeFilePath)
    {
        var normalizedRelativePath = NormalizeRelativeFilePath(relativeFilePath);
        var relativeOsPath = normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar);
        var projectRootFullPath = EnsureTrailingSeparator(Path.GetFullPath(projectRootPath));
        var fileFullPath = Path.GetFullPath(Path.Combine(projectRootFullPath, relativeOsPath));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (!fileFullPath.StartsWith(projectRootFullPath, comparison))
        {
            throw new DomainException("Project files must stay inside the workspace root.");
        }

        return fileFullPath;
    }

    private static string NormalizeRelativeFilePath(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
        {
            throw new DomainException("Project file path is required before syncing to disk.");
        }

        var normalizedPath = relativeFilePath.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalizedPath) || normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new DomainException("Project files must use relative paths inside the workspace.");
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            throw new DomainException("Project file path is required before syncing to disk.");
        }

        foreach (var segment in segments)
        {
            if (segment is "." or "..")
            {
                throw new DomainException("Project files cannot use '.' or '..' path segments.");
            }

            if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new DomainException($"Project file path segment '{segment}' contains invalid characters.");
            }
        }

        return string.Join('/', segments);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
