using System;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Hosting.Logging;

/// <summary>
/// Extension to create the standard boot logger used during application startup.
/// </summary>
public static class BootLoggerExtensions
{
    /// <summary>
    /// Create the boot logger used during application startup.
    /// </summary>
    /// <param name="configure">Optional additional logger-builder configuration.</param>
    /// <returns>The configured boot logger.</returns>
    public static ILogger CreateStandardBootLogger(Action<ILoggingBuilder>? configure = null)
        => LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddTerseConsoleLogFormatter(options => options.IncludeScopes = false);
            builder.AddConsole();

            configure?.Invoke(builder);
        }).CreateLogger("Boot");
}