using NanoAgent.Builder.Application.LLM;

namespace NanoAgent.Builder.Application.Abstractions;

public interface ILLMProvider
{
    IAsyncEnumerable<LLMStreamEvent> GenerateFilePatchesAsync(
        LLMGenerationRequest request,
        CancellationToken cancellationToken = default);
}
