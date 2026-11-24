using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Cors.Infrastructure;

using DotNetNuxt.Abstractions.Options;

namespace DotNetNuxt.Hosting;

/// <summary>
/// Extension methods for configuring hosting-related services.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Add standard startup options to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The configured startup options.</returns>
    public static StartupOptions AddStandardStartupOptions(this IHostApplicationBuilder builder)
    {
        StartupOptions startupOptions = new();
        builder.Configuration.Bind(StartupOptions.Section, startupOptions);
        builder.Services.Configure<StartupOptions>(builder.Configuration.GetSection(StartupOptions.Section));

        return startupOptions;
    }

    /// <summary>
    /// Add a standard CORS policy to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="allowedOrigins">The list of allowed origins for the CORS policy.</param>
    /// <returns>The configured service collection.</returns>
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
