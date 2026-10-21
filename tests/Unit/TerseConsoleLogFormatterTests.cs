using System;
using System.IO;
using DotNetNuxt.Hosting.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetNuxt.Tests.Unit;

public class TerseConsoleLogFormatterTests
{
    [Test]
    public void IncludeFullException_defaults_to_false()
    {
        Assert.That(new TerseConsoleLogOptions().IncludeFullException, Is.False);
    }

    [Test]
    public void Write_by_default_preserves_terse_exception_output()
    {
        var exception = CreateException();

        var output = Format(new TerseConsoleLogOptions(), exception);

        var expected = $"                EXCEPTION InvalidOperationException: Outer failure{Environment.NewLine}"
            + $"{exception.StackTrace}{Environment.NewLine}{Environment.NewLine}";
        Assert.That(output, Does.EndWith(expected));
        Assert.That(output, Does.Not.Contain("Inner failure"));
    }

    [Test]
    public void Write_with_full_exception_includes_exact_exception_string()
    {
        var exception = CreateException();

        var output = Format(new TerseConsoleLogOptions { IncludeFullException = true }, exception);

        Assert.That(output, Does.EndWith(
            $"{exception}{Environment.NewLine}{Environment.NewLine}"));
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
            $"{exception}{Environment.NewLine}{Environment.NewLine}"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Write_without_exception_preserves_message_output(bool includeFullException)
    {
        var output = Format(new TerseConsoleLogOptions { IncludeFullException = includeFullException }, null);

        Assert.That(output, Does.StartWith("ERR "));
        Assert.That(output, Does.EndWith($" [0042] Tests.Category: Log message{Environment.NewLine}"));
    }

    private static string Format(TerseConsoleLogOptions options, Exception? exception)
    {
        using var formatter = new TerseConsoleLogFormatter(Options.Create(options));
        using var writer = new StringWriter();
        var entry = new LogEntry<string>(
            LogLevel.Error, "Tests.Category", new EventId(42), "Log message",
            exception, (state, _) => state);

        formatter.Write(in entry, null, writer);

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
