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
}
