using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Infrastructure.Payments;

internal sealed class StripeWebhookHandler : IStripeWebhookHandler
{
    private static readonly TimeSpan SignatureTolerance = TimeSpan.FromMinutes(5);

    private readonly ILogger<StripeWebhookHandler> _logger;
    private readonly StripeOptions _options;
    private readonly ISubscriptionProvisioningService _subscriptions;

    public StripeWebhookHandler(
        IOptions<StripeOptions> options,
        ISubscriptionProvisioningService subscriptions,
        ILogger<StripeWebhookHandler> logger)
    {
        _options = options.Value;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    public async Task HandleAsync(
        string payload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        EnsureValidSignature(payload, stripeSignatureHeader);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = GetString(root, "type");

        if (!TryGetDataObject(root, out var dataObject))
        {
            throw new DomainException("Stripe webhook payload does not contain data.object.");
        }

        switch (eventType)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(dataObject, cancellationToken);
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
                await HandleSubscriptionChangedAsync(dataObject, cancellationToken);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(dataObject, cancellationToken);
                break;

            default:
                _logger.LogInformation("Ignoring Stripe webhook event type {StripeEventType}.", eventType);
                break;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(JsonElement session, CancellationToken cancellationToken)
    {
        var mode = GetString(session, "mode");
        if (!string.Equals(mode, "subscription", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var paymentStatus = GetString(session, "payment_status");
        if (!string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(paymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var request = new PaidSubscriptionProvisioningRequest(
            UserId: GetString(session, "client_reference_id") ?? GetMetadataString(session, "user_id"),
            PlanCode: GetMetadataString(session, "plan_code"),
            StripeCustomerId: GetStripeId(session, "customer"),
            StripeSubscriptionId: GetStripeId(session, "subscription"),
            StripePriceId: null,
            CurrentPeriodEndsAtUtc: null);

        await _subscriptions.ActivatePaidSubscriptionAsync(request, cancellationToken);
    }

    private async Task HandleSubscriptionChangedAsync(JsonElement subscription, CancellationToken cancellationToken)
    {
        var status = GetString(subscription, "status");
        var request = new PaidSubscriptionProvisioningRequest(
            UserId: GetMetadataString(subscription, "user_id"),
            PlanCode: GetMetadataString(subscription, "plan_code"),
            StripeCustomerId: GetStripeId(subscription, "customer"),
            StripeSubscriptionId: GetString(subscription, "id"),
            StripePriceId: GetFirstPriceId(subscription),
            CurrentPeriodEndsAtUtc: GetUnixTimestamp(subscription, "current_period_end"));

        switch (status)
        {
            case "active":
            case "trialing":
                await _subscriptions.ActivatePaidSubscriptionAsync(request, cancellationToken);
                break;

            case "past_due":
            case "unpaid":
                await _subscriptions.MarkPaidSubscriptionPastDueAsync(
                    ToStateChangeRequest(request),
                    cancellationToken);
                break;

            case "incomplete":
                await _subscriptions.MarkPaidSubscriptionIncompleteAsync(
                    ToStateChangeRequest(request),
                    cancellationToken);
                break;

            case "canceled":
            case "incomplete_expired":
                await _subscriptions.CancelPaidSubscriptionAsync(
                    ToStateChangeRequest(request),
                    cancellationToken);
                break;

            default:
                _logger.LogInformation("Ignoring Stripe subscription status {StripeSubscriptionStatus}.", status);
                break;
        }
    }

    private async Task HandleSubscriptionDeletedAsync(JsonElement subscription, CancellationToken cancellationToken)
    {
        var request = new StripeSubscriptionStateChangeRequest(
            UserId: GetMetadataString(subscription, "user_id"),
            PlanCode: GetMetadataString(subscription, "plan_code"),
            StripeSubscriptionId: GetString(subscription, "id"),
            StripePriceId: GetFirstPriceId(subscription),
            CurrentPeriodEndsAtUtc: GetUnixTimestamp(subscription, "current_period_end") ??
                                    GetUnixTimestamp(subscription, "ended_at") ??
                                    GetUnixTimestamp(subscription, "canceled_at"));

        await _subscriptions.CancelPaidSubscriptionAsync(request, cancellationToken);
    }

    private void EnsureValidSignature(string payload, string stripeSignatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            throw new DomainException("Stripe:WebhookSecret is not configured.");
        }

        if (string.IsNullOrWhiteSpace(stripeSignatureHeader))
        {
            throw new DomainException("Missing Stripe-Signature header.");
        }

        long? timestamp = null;
        var signatures = new List<string>();

        foreach (var part in stripeSignatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length != 2)
            {
                continue;
            }

            if (keyValue[0] == "t" && long.TryParse(keyValue[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedTimestamp))
            {
                timestamp = parsedTimestamp;
            }
            else if (keyValue[0] == "v1")
            {
                signatures.Add(keyValue[1]);
            }
        }

        if (timestamp is null || signatures.Count == 0)
        {
            throw new DomainException("Invalid Stripe-Signature header.");
        }

        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));

        var timestampUtc = DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);
        if (DateTimeOffset.UtcNow - timestampUtc > SignatureTolerance)
        {
            throw new DomainException("Stripe webhook signature timestamp is outside the allowed tolerance.");
        }

        foreach (var signature in signatures)
        {
            if (TryParseHex(signature, out var actualBytes) &&
                actualBytes.Length == expectedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes))
            {
                return;
            }
        }

        throw new DomainException("Invalid Stripe webhook signature.");
    }

    private static StripeSubscriptionStateChangeRequest ToStateChangeRequest(PaidSubscriptionProvisioningRequest request) =>
        new(
            request.UserId,
            request.PlanCode,
            request.StripeSubscriptionId,
            request.StripePriceId,
            request.CurrentPeriodEndsAtUtc);

    private static bool TryGetDataObject(JsonElement root, out JsonElement dataObject)
    {
        dataObject = default;
        if (!root.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("object", out dataObject) ||
            dataObject.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return true;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    private static string? GetStripeId(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return property.ValueKind == JsonValueKind.Object ? GetString(property, "id") : null;
    }

    private static string? GetMetadataString(JsonElement element, string key)
    {
        if (!element.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return metadata.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? GetFirstPriceId(JsonElement subscription)
    {
        if (!subscription.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Object ||
            !items.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Array ||
            data.GetArrayLength() == 0)
        {
            return null;
        }

        var firstItem = data[0];
        return firstItem.TryGetProperty("price", out var price) && price.ValueKind == JsonValueKind.Object
            ? GetString(price, "id")
            : null;
    }

    private static DateTimeOffset? GetUnixTimestamp(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.TryGetInt64(out var seconds) && seconds > 0
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
    }

    private static bool TryParseHex(string hex, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
