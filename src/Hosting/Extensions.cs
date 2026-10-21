using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Cors.Infrastructure;

using DotNetNuxt.Hosting.Options;

namespace DotNetNuxt.Hosting;

public static class HostingExtensions
{
    public static StartupOptions AddStandardStartupOptions(this IHostApplicationBuilder builder)
    {
        StartupOptions startupOptions = new();
        builder.Configuration.Bind(StartupOptions.Section, startupOptions);
        builder.Services.Configure<StartupOptions>(builder.Configuration.GetSection(StartupOptions.Section));

        return startupOptions;
    }

    public static IServiceCollection AddStandardCorsPolicy(this IServiceCollection services, string[] allowedOrigins)
    {
        // No logger exists at registration time, so resolve one lazily from DI
        services.AddOptions<CorsOptions>().Configure<ILoggerFactory>((options, loggerFactory) =>
        {
            if (allowedOrigins is null || allowedOrigins.Length == 0)
            {
                loggerFactory.CreateLogger("DotNetNuxt.Hosting.Cors")
                    .LogWarning("No allowed origins specified; CORS policy not configured");
                return;
            }

            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    // Expose content-disposition header for file downloads
                    // so the client can read the filename from the response
                    .WithExposedHeaders("content-disposition");
            });
        });

        services.AddCors();

        return services;
    }
}
