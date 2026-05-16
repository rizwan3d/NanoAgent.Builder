namespace NanoAgent.Builder.Infrastructure.LLM;

public sealed class AcpAgentOptions
{
    public const string SectionName = "AcpAgent";

    public string Command { get; set; } = "nanoai";

    public string[] Arguments { get; set; } = ["--acp", "--yes", "--surface", "builder"];

    public int RequestTimeoutSeconds { get; set; } = 600;

    public int StartupTimeoutSeconds { get; set; } = 60;

    public bool WriteWorkspaceMemory { get; set; } = true;
}
