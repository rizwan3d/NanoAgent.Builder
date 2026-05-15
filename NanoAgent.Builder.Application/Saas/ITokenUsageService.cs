namespace NanoAgent.Builder.Application.Saas;

public interface ITokenUsageService
{
    Task<TokenUsageDto?> GetCurrentUsageForCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<TokenUsageDto> GetCurrentUsageForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task EnsureModelAllowedAsync(string userId, string llmModel, CancellationToken cancellationToken = default);

    Task EnsureCanUseTokensAsync(
        string userId,
        string llmModel,
        int requestedTokens,
        CancellationToken cancellationToken = default);

    Task<TokenUsageDto> RecordUsageAsync(
        string userId,
        string llmModel,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken = default);
}
