using Kmz2Mbtiles.Core.Abstractions;
using Spectre.Console;

namespace Kmz2Mbtiles.Cli;

/// <summary>
/// <see cref="IProgressReporter"/> implementation that renders pipeline output using
/// Spectre.Console markup.
/// </summary>
/// <remarks>
/// All messages passed to this reporter are escaped before being written, so callers may
/// pass arbitrary text (file names, exception messages, GDAL output) without worrying
/// about markup injection or parse errors.
/// </remarks>
public sealed class SpectreProgressReporter : IProgressReporter
{
    /// <inheritdoc/>
    public void Header(string title)
    {
        AnsiConsole.MarkupLine("[grey]" + new string('=', 60) + "[/]");
        AnsiConsole.MarkupLine($"[grey]{title.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[grey]" + new string('=', 60) + "[/]");
    }

    /// <inheritdoc/>
    public void Step(int current, int total, string message)
    {
        AnsiConsole.MarkupLine($"\n[grey][[{current}/{total}]][/] {message.EscapeMarkup()}");
    }

    /// <inheritdoc/>
    public void Info(string message)
    {
        AnsiConsole.MarkupLine($"  [grey]{message.EscapeMarkup()}[/]");
    }

    /// <inheritdoc/>
    public void Detail(string message)
    {
        AnsiConsole.MarkupLine($"[grey]          {message.EscapeMarkup()}[/]");
    }

    /// <inheritdoc/>
    public void Path(string label, string path)
    {
        AnsiConsole.MarkupLine($"[grey]          {label,-10}: {path.EscapeMarkup()}[/]");
    }

    /// <inheritdoc/>
    public void Success(string message)
    {
        AnsiConsole.MarkupLine($"[bold green][[SUCCESS]][/] {message.EscapeMarkup()}");
    }

    /// <inheritdoc/>
    public void Warn(string message)
    {
        AnsiConsole.MarkupLine($"[yellow][[WARN]][/] {message.EscapeMarkup()}");
    }

    /// <inheritdoc/>
    public void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]{message.EscapeMarkup()}[/]");
    }
}