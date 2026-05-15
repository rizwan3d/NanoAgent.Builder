using NanoAgent.Builder.Application;
using NanoAgent.Builder.Infrastructure;
using NanoAgent.Builder.Infrastructure.Database;

namespace NanoAgent.Builder;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

        var app = builder.Build();

        await app.InitialiseDatabaseAsync();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        await app.RunAsync();
    }
}
