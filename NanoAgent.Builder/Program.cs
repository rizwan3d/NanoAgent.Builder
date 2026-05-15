using NanoAgent.Builder.Application;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Infrastructure;
using NanoAgent.Builder.Infrastructure.Database;
using NanoAgent.Builder.Security;

namespace NanoAgent.Builder;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizePage("/Index");
            options.Conventions.AuthorizePage("/Workspace");
            options.Conventions.AuthorizeFolder("/Admin");
            options.Conventions.AuthorizeFolder("/Billing");
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        var app = builder.Build();

        await app.InitialiseDatabaseAsync();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapPost("/billing/stripe-webhook", async (HttpRequest request, IStripeWebhookHandler webhookHandler, CancellationToken cancellationToken) =>
        {
            using var reader = new StreamReader(request.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var signature = request.Headers["Stripe-Signature"].ToString();

            try
            {
                await webhookHandler.HandleAsync(payload, signature, cancellationToken);
                return Results.Ok();
            }
            catch (DomainException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        })
        .AllowAnonymous();


        app.MapPost("/api/usage/record", async (
            RecordTokenUsageRequest request,
            ICurrentUserContext currentUser,
            ITokenUsageService tokenUsageService,
            CancellationToken cancellationToken) =>
        {
            if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var usage = await tokenUsageService.RecordUsageAsync(
                    currentUser.UserId,
                    request.LlmModel,
                    request.InputTokens,
                    request.OutputTokens,
                    cancellationToken);

                return Results.Ok(usage);
            }
            catch (DomainException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        })
        .RequireAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        await app.RunAsync();
    }

    public sealed record RecordTokenUsageRequest(
        string LlmModel,
        int InputTokens,
        int OutputTokens);
}
