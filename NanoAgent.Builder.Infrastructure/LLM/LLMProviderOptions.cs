namespace NanoAgent.Builder.Infrastructure.LLM;

public sealed class LLMProviderOptions
{
    public const string SectionName = "LLMProvider";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string CompletionPath { get; set; } = "/v1/chat/completions";

    public int TimeoutSeconds { get; set; } = 120;

    public bool Stream { get; set; } = false;
}
