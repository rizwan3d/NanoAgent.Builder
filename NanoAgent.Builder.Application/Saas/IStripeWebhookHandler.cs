namespace NanoAgent.Builder.Application.Saas;

public interface IStripeWebhookHandler
{
    Task HandleAsync(
        string payload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default);
}
