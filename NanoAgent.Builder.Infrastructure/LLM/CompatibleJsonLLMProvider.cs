using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.LLM;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Infrastructure.LLM;

internal sealed class CompatibleJsonLLMProvider : ILLMProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;
    private readonly LLMProviderOptions _options;

    public CompatibleJsonLLMProvider(HttpClient httpClient, IOptions<LLMProviderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async IAsyncEnumerable<LLMStreamEvent> GenerateFilePatchesAsync(
        LLMGenerationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        using var httpRequest = BuildRequest(request);
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(10, _options.TimeoutSeconds)));
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            linkedSource.Token);

        var responseBody = await response.Content.ReadAsStringAsync(linkedSource.Token);
        if (!response.IsSuccessStatusCode)
        {
            var detail = string.IsNullOrWhiteSpace(responseBody)
                ? $"The provider returned HTTP {(int)response.StatusCode}."
                : $"The provider returned HTTP {(int)response.StatusCode}: {Truncate(responseBody, 300)}";
            throw new DomainException(detail);
        }

        var content = _options.Stream
            ? ExtractTextFromStream(responseBody)
            : ExtractTextFromResponse(responseBody);

        if (!string.IsNullOrWhiteSpace(content))
        {
            yield return new LLMTextDelta(content);
        }

        var parsed = ParsePatchEnvelope(content);
        foreach (var patch in parsed.Patches)
        {
            yield return new LLMFilePatchDelta(patch);
        }

        yield return new LLMGenerationCompleted(parsed.Patches, parsed.InputTokens, parsed.OutputTokens);
    }

    private HttpRequestMessage BuildRequest(LLMGenerationRequest request)
    {
        var endpoint = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), _options.CompletionPath.TrimStart('/'));
        var payload = new
        {
            model = request.RequestedModel,
            stream = _options.Stream,
            temperature = 0.2,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = BuildSystemPrompt()
                },
                new
                {
                    role = "user",
                    content = BuildUserPrompt(request)
                }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return httpRequest;
    }

    private static string BuildSystemPrompt() =>
        "You are a software workspace generator. Return only JSON with this shape: " +
        "{\"summary\":\"short summary\",\"inputTokens\":number,\"outputTokens\":number," +
        "\"patches\":[{\"path\":\"relative file path\",\"language\":\"language id\",\"changeKind\":\"upsert\",\"content\":\"complete file content\"}]}. " +
        "Use complete file contents for every patch. Keep paths relative. Do not include markdown fences.";

    private static string BuildUserPrompt(LLMGenerationRequest request)
    {
        var fileSummaries = request.Files
            .Select(file => new
            {
                file.Path,
                file.Language,
                Content = Truncate(file.Content, 12000)
            })
            .ToList();

        var payload = new
        {
            request.ProjectId,
            request.ProjectName,
            request.ProjectDescription,
            request.RequestedModel,
            UserRequest = request.UserMessage,
            Files = fileSummaries
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string ExtractTextFromResponse(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString() ?? string.Empty;
                }

                if (choice.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString() ?? string.Empty;
                }
            }
        }

        return responseBody;
    }

    private static string ExtractTextFromStream(string responseBody)
    {
        var builder = new StringBuilder();
        using var reader = new StringReader(responseBody);
        while (reader.ReadLine() is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (string.IsNullOrWhiteSpace(data) || data == "[DONE]")
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(data);
                var root = document.RootElement;
                if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.String)
                    {
                        builder.Append(content.GetString());
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        return builder.ToString();
    }

    private static PatchEnvelope ParsePatchEnvelope(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("The provider returned an empty response.");
        }

        var json = ExtractJsonObject(content.Trim());
        var envelope = JsonSerializer.Deserialize<PatchEnvelope>(json, JsonOptions);
        if (envelope is null || envelope.Patches.Count == 0)
        {
            throw new DomainException("The provider did not return any file patches.");
        }

        return envelope with
        {
            Patches = envelope.Patches
                .Where(IsUsablePatch)
                .Select(NormalizePatch)
                .ToList()
        };
    }

    private static bool IsUsablePatch(GeneratedFilePatch patch) =>
        !string.IsNullOrWhiteSpace(patch.Path) &&
        !string.IsNullOrWhiteSpace(patch.Content);

    private static GeneratedFilePatch NormalizePatch(GeneratedFilePatch patch) =>
        patch with
        {
            Path = patch.Path.Trim().Replace('\\', '/'),
            Language = string.IsNullOrWhiteSpace(patch.Language) ? "plaintext" : patch.Language.Trim(),
            ChangeKind = string.IsNullOrWhiteSpace(patch.ChangeKind) ? "upsert" : patch.ChangeKind.Trim().ToLowerInvariant()
        };

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new DomainException("The provider response did not contain a JSON object.");
        }

        return content[start..(end + 1)];
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new DomainException("LLMProvider:BaseUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new DomainException("LLMProvider:ApiKey is not configured.");
        }
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength] + "\n... truncated ...";
    }

    private sealed record PatchEnvelope(
        string? Summary,
        int InputTokens,
        int OutputTokens,
        IReadOnlyList<GeneratedFilePatch> Patches);
}
