using System.Diagnostics;
using System.Text;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Infrastructure.Workspaces;

public sealed class ProjectWorkspaceSetupRunner : IProjectWorkspaceSetupRunner
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(5);
    private readonly IProjectWorkspaceFileSystem _workspaceFileSystem;

    public ProjectWorkspaceSetupRunner(IProjectWorkspaceFileSystem workspaceFileSystem)
    {
        _workspaceFileSystem = workspaceFileSystem;
    }

    public async Task<ProjectWorkspaceSetupResult> PrepareAsync(
        AgentProject project,
        CancellationToken cancellationToken = default)
    {
        var projectRootPath = _workspaceFileSystem.GetProjectRootPath(project);
        var commands = new List<ProjectWorkspaceCommandResult>();

        foreach (var command in new[]
        {
            new WorkspaceCommand("npm install"),
            new WorkspaceCommand("npm run build")
        })
        {
            var result = await RunCommandAsync(projectRootPath, command, cancellationToken);
            commands.Add(result);

            if (result.ExitCode != 0)
            {
                return new ProjectWorkspaceSetupResult(
                    false,
                    commands,
                    $"Workspace setup stopped because `{result.Command}` returned exit code {result.ExitCode}.");
            }
        }

        return new ProjectWorkspaceSetupResult(true, commands, null);
    }

    private static async Task<ProjectWorkspaceCommandResult> RunCommandAsync(
        string workingDirectory,
        WorkspaceCommand command,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(CommandTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        var output = new StringBuilder();
        var error = new StringBuilder();

        var startInfo = CreateStartInfo(workingDirectory, command);

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                error.AppendLine(args.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                return new ProjectWorkspaceCommandResult(command.DisplayName, -1, string.Empty, "The process could not be started.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(linkedSource.Token);

            return new ProjectWorkspaceCommandResult(
                command.DisplayName,
                process.ExitCode,
                output.ToString(),
                error.ToString());
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKillProcess(process);

            return new ProjectWorkspaceCommandResult(
                command.DisplayName,
                -1,
                output.ToString(),
                $"The command timed out after {CommandTimeout.TotalMinutes:N0} minutes.{Environment.NewLine}{error}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKillProcess(process);

            return new ProjectWorkspaceCommandResult(
                command.DisplayName,
                -1,
                output.ToString(),
                $"The command was cancelled.{Environment.NewLine}{error}");
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new ProjectWorkspaceCommandResult(
                command.DisplayName,
                -1,
                output.ToString(),
                exception.Message);
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string workingDirectory,
        WorkspaceCommand command)
    {
        if (OperatingSystem.IsWindows())
        {
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command.CommandText}",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        return new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = $"-c \"{command.CommandText.Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore cleanup failures. The command result will still report the original failure.
        }
    }

    private sealed record WorkspaceCommand(string CommandText)
    {
        public string DisplayName => CommandText;
    }
}
