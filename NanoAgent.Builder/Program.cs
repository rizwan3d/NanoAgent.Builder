using NanoAgent.Builder.Application;
using NanoAgent.Builder.Application.Abstractions;
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

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        await app.RunAsync();
    }
}
