using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ASVSGuard.Core.Interfaces;
using ASVSGuard.Core.Services;
using ASVSGuard.Infrastructure.Data;
using ASVSGuard.Infrastructure.Parsers;
using Microsoft.Extensions.DependencyInjection;

namespace ASVSGuard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connStr = Environment.GetEnvironmentVariable("DATABASE_URL")
                      ?? config.GetConnectionString("DefaultConnection");

        // Railway DATABASE_URL is postgresql://user:pass@host:port/db — convert to Npgsql format
        if (connStr is not null && connStr.StartsWith("postgresql://"))
            connStr = ConvertRailwayUrl(connStr);

        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(connStr));

        services.AddIdentity<IdentityUser, IdentityRole>(opts =>
        {
            opts.Password.RequireDigit = true;
            opts.Password.RequiredLength = 12;
            opts.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IExigenceRepository, ExigenceRepository>();
        services.AddScoped<IRepoScanRepository, RepoScanRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IExcelParser, ExcelExigenceParser>();

        // Use Groq if an API key is configured, otherwise fall back to HuggingFace router
        var groqKey   = config["Groq:ApiKey"];
        var aiBaseUrl = !string.IsNullOrWhiteSpace(groqKey)
            ? "https://api.groq.com"
            : "https://router.huggingface.co";

        services.AddHttpClient<IAIHttpClient, HuggingFaceClient>(client =>
        {
            client.BaseAddress = new Uri(aiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddHttpClient<GitHubMcpClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ExigenceService>();
        services.AddScoped<AIService>();
        services.AddScoped<RepoScanService>();
        services.AddScoped<ChatService>();
        return services;
    }

    private static string ConvertRailwayUrl(string url)
    {
        // postgresql://user:pass@host:port/db  →  Host=host;Port=port;Database=db;Username=user;Password=pass
        var uri = new Uri(url);
        var colonIndex = uri.UserInfo.IndexOf(':');
        var user = colonIndex >= 0 ? uri.UserInfo[..colonIndex] : uri.UserInfo;
        var pass = colonIndex >= 0 ? uri.UserInfo[(colonIndex + 1)..] : string.Empty;
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }
}
