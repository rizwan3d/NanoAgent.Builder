namespace NanoAgent.Builder.Infrastructure.Payments;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;

    public Dictionary<string, string> Prices { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string? GetPriceId(string planCode)
    {
        return Prices.TryGetValue(planCode, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }
}
