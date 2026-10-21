using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetNuxt.Hosting.Logging;

/// <summary>
/// Formats logs with just the information we want in local debugging
/// </summary>
/// <remarks>
/// Domain-agnostic. Could be broken out into separate reusable library.
///
/// One thing to be aware of, we do use the TestName which is set by
/// the test correlation middleware during test execution.
///
/// Continuation lines in messages and exceptions are indented with a tab.
/// </remarks>
public sealed partial class TerseConsoleLogFormatter : ConsoleFormatter, IDisposable
{
    [GeneratedRegex("(.+?) \\(.+?\\)$")]
    private static partial Regex ActionRegex();

    private readonly TerseConsoleLogOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    public TerseConsoleLogFormatter(IOptions<TerseConsoleLogOptions> options)
        // Case insensitive
        : base("TerseConsole")
    {
        _options = options.Value;
    }

    /// <summary>
    /// Write a log message
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="logEntry"></param>
    /// <param name="scopeProvider"></param>
    /// <param name="textWriter"></param>
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message =
            logEntry.Formatter?.Invoke(
                logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        var category = logEntry.Category;
        var testName = default(string);
        var traceId = default(string);

        var collectedScopes = new List<KeyValuePair<string, object>>();

        scopeProvider?.ForEachScope((x, t) =>
        {
            if (x is IEnumerable<KeyValuePair<string, object>> values)
            {
                // TODO: I don't think we're setting ActionName right now
                var found = values.Where(y => y.Key == "ActionName");
                if (found.Any())
                {
                    var kvp = found.FirstOrDefault();
                    var action = kvp.Value?.ToString() ?? string.Empty;

                    var match = ActionRegex().Match(action);
                    if (match.Success)
                    {
                        var first = match.Groups[1].Value;
                        var split = first.Split('.');
                        var chosen = split[^2..];
                        category = string.Join('.', chosen).Replace("Controller", "");
                    }
                }
                found = values.Where(y => y.Key == "TestName");
                if (found.Any())
                {
                    var kvp = found.FirstOrDefault();
                    testName = kvp.Value?.ToString();
                }

                if (_options.IncludeTraceId && traceId == null)
                {
                    foreach (var kvp in values)
                    {
                        if (kvp.Key == "TraceId" && traceId == null)
                        {
                            traceId = kvp.Value?.ToString();
                        }
                    }
                }

                collectedScopes.AddRange(values);
            }
        }, typeof(TState));

        textWriter.Write(logEntry.LogLevel.ToString().ToUpper()[0..3]);
        if (_options.TimestampFormat is string timestampFormat)
        {
            var timestamp = _options.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
            textWriter.Write(' ');
            textWriter.Write(timestamp.ToString(timestampFormat));
        }

        textWriter.Write
        (
            " [{0:D4}] {1}{2}{3}: ",
            category.StartsWith("Microsoft")
                ? "****"
                : logEntry.EventId.Id.ToString("D4"),
            testName != null
                ? $"/{testName}/ "
                : string.Empty,
            category,
            !string.IsNullOrEmpty(traceId)
                ? $" {traceId}"
                : string.Empty
        );
        textWriter.WriteLine(IndentContinuationLines(message));

        if (logEntry.Exception != null)
        {
            if (_options.IncludeFullException)
            {
                textWriter.WriteLine(IndentContinuationLines(logEntry.Exception.ToString()));
            }
            else
            {
                var exceptionText = $"                EXCEPTION {logEntry.Exception.GetType().Name}: {logEntry.Exception.Message}"
                    + Environment.NewLine + logEntry.Exception.StackTrace;
                textWriter.WriteLine(IndentContinuationLines(exceptionText));
            }
            textWriter.WriteLine();
        }

        if (_options.IncludeScopes)
        {
            foreach (var kvp in collectedScopes)
            {
                textWriter.WriteLine("    {0}: {1}", kvp.Key, kvp.Value);
            }
        }

    }

    private static string IndentContinuationLines(string text) => text.Replace("\n", "\n\t");

    /*
     * EXAMPLE SCOPES:
        SpanId: b6e2eeaa4c526aa4
        TraceId: b4982c97c45f36e99f70f23fe7d78020
        ParentId: 0000000000000000
        ConnectionId: 0HN1F7CSLC00V
        RequestId: 0HN1F7CSLC00V:0000006D
        RequestPath: /api/Identity/refresh
        ActionId: a74f05e9-2d99-4455-9491-01c2f58cc868
        ActionName: Common.Identity.Controllers.IdentityController.Refresh (Common.Identity)     * 
     */
    private void CustomLogicGoesHere(TextWriter textWriter)
    {
    }

    /// <summary>
    /// Dispose object
    /// </summary>
    public void Dispose() { }
}

/// <summary>
/// Formatter options
/// </summary>
public sealed class TerseConsoleLogOptions : ConsoleFormatterOptions
{
    /// <summary>
    /// Creates options with the default local timestamp format, "MM/dd HH:mm:ss".
    /// </summary>
    /// <remarks>
    /// Set the inherited TimestampFormat to customize timestamps, or null to omit
    /// them. Header spacing is supplied by the formatter, so no trailing space is
    /// needed. Set UseUtcTimestamp to true to use UTC instead of local time.
    /// </remarks>
    public TerseConsoleLogOptions()
    {
        TimestampFormat = "MM/dd HH:mm:ss";
    }

    /// <summary>
    /// Gets or sets whether to include the scope's TraceId after the category
    /// on the main log line. Defaults to false.
    /// </summary>
    /// <remarks>
    /// Independent of IncludeScopes, which controls the separate scope dump.
    /// Uses the first non-null TraceId from outermost to innermost scopes.
    /// Missing or empty trace IDs do not change the output.
    /// </remarks>
    public bool IncludeTraceId { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to render exceptions using their full string representation,
    /// including inner exceptions and their stack traces, with tab-indented continuation
    /// lines. Defaults to false.
    /// </summary>
    /// <remarks>
    /// Leave disabled for terse console output when another logging sink, such as
    /// Aspire or Application Insights, captures full exception details.
    /// </remarks>
    public bool IncludeFullException { get; set; } = false;
}

/// <summary>
/// Extension to add and select the custom console formatter
/// </summary>
public static class ConsoleLoggerExtensions
{
    /// <summary>
    /// Add console logging and select the Terse Console Log formatter.
    /// </summary>
    /// <remarks>
    /// Configures the formatter in code; Logging:Console settings are not required.
    /// </remarks>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static ILoggingBuilder AddTerseConsoleLogFormatter(
        this ILoggingBuilder builder,
        Action<TerseConsoleLogOptions> configure) =>
        builder.AddConsole(options => options.FormatterName = "TerseConsole")
            .AddConsoleFormatter<TerseConsoleLogFormatter, TerseConsoleLogOptions>(configure);
}
