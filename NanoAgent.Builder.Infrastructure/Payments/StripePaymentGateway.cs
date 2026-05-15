using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Infrastructure.Identity;
using Stripe;
using BillingPortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using BillingPortalSessionService = Stripe.BillingPortal.SessionService;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionSubscriptionDataOptions = Stripe.Checkout.SessionSubscriptionDataOptions;

namespace NanoAgent.Builder.Infrastructure.Payments;

internal sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;
    private readonly UserManager<ApplicationUser> _userManager;

    public StripePaymentGateway(
        IOptions<StripeOptions> options,
        UserManager<ApplicationUser> userManager)
    {
        _options = options.Value;
        _userManager = userManager;
    }

    public async Task<PaymentCheckoutSession> CreateSubscriptionCheckoutSessionAsync(
        PaymentCheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureStripeSecretConfigured();

        var user = await FindUserAsync(request.UserId);
        var stripeClient = new StripeClient(_options.SecretKey);
        var customerId = await GetOrCreateCustomerIdAsync(stripeClient, user, request.UserEmail, cancellationToken);

        var sessionService = new CheckoutSessionService(stripeClient);
        var session = await sessionService.CreateAsync(new CheckoutSessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            ClientReferenceId = user.Id,
            SuccessUrl = request.SuccessUrl.ToString(),
            CancelUrl = request.CancelUrl.ToString(),
            AllowPromotionCodes = true,
            LineItems = new List<CheckoutSessionLineItemOptions>
            {
                new()
                {
                    Price = request.StripePriceId,
                    Quantity = 1
                }
            },
            Metadata = BuildMetadata(user.Id, request.PlanCode),
            SubscriptionData = new CheckoutSessionSubscriptionDataOptions
            {
                Metadata = BuildMetadata(user.Id, request.PlanCode)
            }
        }, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new DomainException("Stripe did not return a Checkout redirect URL.");
        }

        return new PaymentCheckoutSession(session.Id, session.Url);
    }

    public async Task<PaymentPortalSession> CreateCustomerPortalSessionAsync(
        string userId,
        Uri returnUrl,
        CancellationToken cancellationToken = default)
    {
        EnsureStripeSecretConfigured();

        var user = await FindUserAsync(userId);
        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            throw new DomainException("No Stripe customer exists for this account yet.");
        }

        var stripeClient = new StripeClient(_options.SecretKey);
        var portalSessionService = new BillingPortalSessionService(stripeClient);
        var portalSession = await portalSessionService.CreateAsync(new BillingPortalSessionCreateOptions
        {
            Customer = user.StripeCustomerId,
            ReturnUrl = returnUrl.ToString()
        }, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(portalSession.Url))
        {
            throw new DomainException("Stripe did not return a Billing Portal redirect URL.");
        }

        return new PaymentPortalSession(portalSession.Url);
    }

    private async Task<ApplicationUser> FindUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user ?? throw new DomainException("The signed-in user could not be found.");
    }

    private async Task<string> GetOrCreateCustomerIdAsync(
        StripeClient stripeClient,
        ApplicationUser user,
        string? currentUserEmail,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            return user.StripeCustomerId;
        }

        var customerService = new CustomerService(stripeClient);
        var customer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = user.Email ?? currentUserEmail,
            Name = user.DisplayName,
            Metadata = new Dictionary<string, string>
            {
                ["user_id"] = user.Id
            }
        }, cancellationToken: cancellationToken);

        user.StripeCustomerId = customer.Id;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(error => error.Description));
            throw new DomainException($"Could not save the Stripe customer id: {errors}");
        }

        return customer.Id;
    }

    private void EnsureStripeSecretConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new DomainException("Stripe:SecretKey is not configured.");
        }
    }

    private static Dictionary<string, string> BuildMetadata(string userId, string planCode) =>
        new(StringComparer.Ordinal)
        {
            ["user_id"] = userId,
            ["plan_code"] = planCode
        };
}
