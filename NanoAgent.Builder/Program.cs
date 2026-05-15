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

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        await app.RunAsync();
    }
}
