using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Abstractions;

public interface IProjectWorkspaceSetupRunner
{
    Task<ProjectWorkspaceSetupResult> PrepareAsync(
        AgentProject project,
        CancellationToken cancellationToken = default);
}

public sealed record ProjectWorkspaceSetupResult(
    bool Succeeded,
    IReadOnlyList<ProjectWorkspaceCommandResult> Commands,
    string? ErrorMessage)
{
    public string ToArtifactContent()
    {
        var lines = new List<string>
        {
            $"Setup status: {(Succeeded ? "completed" : "needs attention")}",
            string.Empty
        };

        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            lines.Add("Error:");
            lines.Add(ErrorMessage.Trim());
            lines.Add(string.Empty);
        }

        foreach (var command in Commands)
        {
            lines.Add($"$ {command.Command}");
            lines.Add($"Exit code: {command.ExitCode}");

            if (!string.IsNullOrWhiteSpace(command.Output))
            {
                lines.Add("Output:");
                lines.Add(command.Output.Trim());
            }

            if (!string.IsNullOrWhiteSpace(command.Error))
            {
                lines.Add("Error output:");
                lines.Add(command.Error.Trim());
            }

            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines).Trim();
    }
}

public sealed record ProjectWorkspaceCommandResult(
    string Command,
    int ExitCode,
    string Output,
    string Error);
