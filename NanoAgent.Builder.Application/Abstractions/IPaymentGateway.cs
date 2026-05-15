namespace NanoAgent.Builder.Application.Abstractions;

public interface IPaymentGateway
{
    Task<PaymentCheckoutSession> CreateSubscriptionCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentPortalSession> CreateCustomerPortalSessionAsync(
        string userId,
        Uri returnUrl,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentCheckoutRequest(
    string UserId,
    string? UserEmail,
    string PlanCode,
    string PlanName,
    string StripePriceId,
    Uri SuccessUrl,
    Uri CancelUrl);

public sealed record PaymentCheckoutSession(string SessionId, string RedirectUrl);

public sealed record PaymentPortalSession(string RedirectUrl);
