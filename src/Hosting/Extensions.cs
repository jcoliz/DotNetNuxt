using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddCors(options =>
        {
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

        return services;
    }
}
