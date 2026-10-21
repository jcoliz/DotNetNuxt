using System;
using System.Collections.Generic;
using System.IO;
using DotNetNuxt.Hosting.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetNuxt.Tests.Unit;

public class TerseConsoleLogFormatterTests
{
    [Test]
    public void IncludeTraceId_defaults_to_false()
    {
        Assert.That(new TerseConsoleLogOptions().IncludeTraceId, Is.False);
    }

    [Test]
    public void Write_by_default_does_not_include_inline_trace_id()
    {
        var scopes = new LoggerExternalScopeProvider();
        using var scope = scopes.Push(new Dictionary<string, object> { ["TraceId"] = "trace-123" });

        var output = Format(new TerseConsoleLogOptions(), null, scopes);

        Assert.That(output, Does.EndWith($" [0042] Tests.Category: Log message{Environment.NewLine}"));
        Assert.That(output, Does.Not.Contain("trace-123"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Write_with_trace_id_is_independent_of_scope_dump(bool includeScopes)
    {
        var scopes = new LoggerExternalScopeProvider();
        using var scope = scopes.Push(new Dictionary<string, object>
        {
            ["TraceId"] = "trace-123",
            ["TestName"] = "ExampleTest",
            ["RequestId"] = "request-456"
        });

        var output = Format(new TerseConsoleLogOptions
        {
            IncludeTraceId = true,
            IncludeScopes = includeScopes
        }, null, scopes);

        Assert.That(output, Does.Contain(
            $" [0042] /ExampleTest/ Tests.Category trace-123: Log message{Environment.NewLine}"));
        if (includeScopes)
        {
            Assert.That(output, Does.Contain($"    TraceId: trace-123{Environment.NewLine}"));
            Assert.That(output, Does.Contain($"    RequestId: request-456{Environment.NewLine}"));
        }
        else
        {
            Assert.That(output.Split(Environment.NewLine), Has.Length.EqualTo(2));
            Assert.That(output, Does.Not.Contain("request-456"));
        }
    }

    [Test]
    public void Write_with_scope_dump_only_preserves_existing_trace_id_output()
    {
        var scopes = new LoggerExternalScopeProvider();
        using var scope = scopes.Push(new Dictionary<string, object> { ["TraceId"] = "trace-123" });

        var output = Format(new TerseConsoleLogOptions { IncludeScopes = true }, null, scopes);

        Assert.That(output, Does.EndWith(
            $" [0042] Tests.Category: Log message{Environment.NewLine}    TraceId: trace-123{Environment.NewLine}"));
    }

    [Test]
    public void Write_with_trace_id_uses_first_non_null_value_from_outer_scopes()
    {
        var scopes = new LoggerExternalScopeProvider();
        using var nullScope = scopes.Push(new Dictionary<string, object> { ["TraceId"] = null! });
        using var outerScope = scopes.Push(new Dictionary<string, object> { ["TraceId"] = "outer-trace" });
        using var innerScope = scopes.Push(new Dictionary<string, object> { ["TraceId"] = "inner-trace" });

        var output = Format(new TerseConsoleLogOptions { IncludeTraceId = true }, null, scopes);

        Assert.That(output, Does.EndWith(
            $" [0042] Tests.Category outer-trace: Log message{Environment.NewLine}"));
        Assert.That(output, Does.Not.Contain("inner-trace"));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Write_with_missing_or_empty_trace_id_preserves_output(string? traceId)
    {
        var scopes = new LoggerExternalScopeProvider();
        using var textScope = scopes.Push("Unstructured scope");
        using var scope = scopes.Push(new Dictionary<string, object> { ["TraceId"] = traceId! });

        var output = Format(new TerseConsoleLogOptions { IncludeTraceId = true }, null, scopes);

        Assert.That(output, Does.EndWith($" [0042] Tests.Category: Log message{Environment.NewLine}"));
    }

    [Test]
    public void Write_with_trace_id_without_scope_provider_preserves_output()
    {
        var output = Format(new TerseConsoleLogOptions { IncludeTraceId = true }, null);

        Assert.That(output, Does.EndWith($" [0042] Tests.Category: Log message{Environment.NewLine}"));
    }

    [Test]
    public void IncludeFullException_defaults_to_false()
    {
        Assert.That(new TerseConsoleLogOptions().IncludeFullException, Is.False);
    }

    [Test]
    public void Write_by_default_indents_terse_exception_stack_trace()
    {
        var exception = CreateException();

        var output = Format(new TerseConsoleLogOptions(), exception);

        var expected = $"                EXCEPTION InvalidOperationException: Outer failure{Environment.NewLine}"
            + $"\t{exception.StackTrace?.Replace("\n", "\n\t")}{Environment.NewLine}{Environment.NewLine}";
        Assert.That(output, Does.EndWith(expected));
        Assert.That(output, Does.Not.Contain("Inner failure"));
    }

    [Test]
    public void Write_with_full_exception_includes_indented_exception_string()
    {
        var exception = CreateException();

        var output = Format(new TerseConsoleLogOptions { IncludeFullException = true }, exception);

        Assert.That(output, Does.EndWith(
            $"{exception.ToString().Replace("\n", "\n\t")}{Environment.NewLine}{Environment.NewLine}"));
        Assert.That(output, Does.Contain("Inner failure"));
        Assert.That(output, Does.Not.Contain("                EXCEPTION"));
    }

    [Test]
    public void Write_with_full_exception_preserves_all_aggregate_inner_exceptions()
    {
        var exception = new AggregateException("Multiple failures",
            CreateException(), new ArgumentException("Another failure"));

        var output = Format(new TerseConsoleLogOptions { IncludeFullException = true }, exception);

        Assert.That(output, Does.EndWith(
            $"{exception.ToString().Replace("\n", "\n\t")}{Environment.NewLine}{Environment.NewLine}"));
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void Write_indents_message_continuation_lines_and_preserves_line_endings(string newline)
    {
        var message = $"First line{newline}Second line{newline}{newline}Fourth line{newline}";

        var output = Format(new TerseConsoleLogOptions(), null, message: message);

        Assert.That(output, Does.EndWith(
            $" [0042] Tests.Category: First line{newline}\tSecond line{newline}\t{newline}\tFourth line{newline}\t{Environment.NewLine}"));
    }

    [TestCase(false, "\n")]
    [TestCase(false, "\r\n")]
    [TestCase(true, "\n")]
    [TestCase(true, "\r\n")]
    public void Write_indents_multiline_exception_messages(bool includeFullException, string newline)
    {
        var exception = new InvalidOperationException($"First line{newline}Second line");

        var output = Format(new TerseConsoleLogOptions { IncludeFullException = includeFullException }, exception);

        Assert.That(output, Does.Contain($"First line{newline}\tSecond line"));
        if (includeFullException)
        {
            Assert.That(output, Does.EndWith(
                $"System.InvalidOperationException: First line{newline}\tSecond line{Environment.NewLine}{Environment.NewLine}"));
        }
        else
        {
            Assert.That(output, Does.EndWith(
                $"                EXCEPTION InvalidOperationException: First line{newline}\tSecond line{Environment.NewLine}\t{Environment.NewLine}{Environment.NewLine}"));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Write_without_exception_preserves_message_output(bool includeFullException)
    {
        var output = Format(new TerseConsoleLogOptions { IncludeFullException = includeFullException }, null);

        Assert.That(output, Does.StartWith("ERR "));
        Assert.That(output, Does.EndWith($" [0042] Tests.Category: Log message{Environment.NewLine}"));
    }

    private static string Format(
        TerseConsoleLogOptions options, Exception? exception, IExternalScopeProvider? scopeProvider = null,
        string message = "Log message")
    {
        using var formatter = new TerseConsoleLogFormatter(Options.Create(options));
        using var writer = new StringWriter();
        var entry = new LogEntry<string>(
            LogLevel.Error, "Tests.Category", new EventId(42), message,
            exception, (state, _) => state);

        formatter.Write(in entry, scopeProvider, writer);

        return writer.ToString();
    }

    private static Exception CreateException()
    {
        try
        {
            try
            {
                throw new ArgumentException("Inner failure");
            }
            catch (ArgumentException inner)
            {
                throw new InvalidOperationException("Outer failure", inner);
            }
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }
}
