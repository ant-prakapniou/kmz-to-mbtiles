using Kmz2Mbtiles.Core.Abstractions;
using Spectre.Console;

namespace Kmz2Mbtiles.Cli;

public sealed class SpectreProgressReporter : IProgressReporter
{
    public void Header(string title)
    {
        AnsiConsole.MarkupLine("[grey]" + new string('=', 60) + "[/]");
        AnsiConsole.MarkupLine($"[grey]{title.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[grey]" + new string('=', 60) + "[/]");
    }

    public void Step(int current, int total, string message)
    {
        AnsiConsole.MarkupLine($"\n[grey][[{current}/{total}]][/] {message.EscapeMarkup()}");
    }

    public void Info(string message)
    {
        AnsiConsole.MarkupLine($"  [grey]{message.EscapeMarkup()}[/]");
    }

    public void Detail(string message)
    {
        AnsiConsole.MarkupLine($"[grey]          {message.EscapeMarkup()}[/]");
    }

    public void Path(string label, string path)
    {
        AnsiConsole.MarkupLine($"[grey]          {label,-10}: {path.EscapeMarkup()}[/]");
    }

    public void Success(string message)
    {
        AnsiConsole.MarkupLine($"[bold green][[SUCCESS]][/] {message.EscapeMarkup()}");
    }

    public void Warn(string message)
    {
        AnsiConsole.MarkupLine($"[yellow][[WARN]][/] {message.EscapeMarkup()}");
    }

    public void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]{message.EscapeMarkup()}[/]");
    }
}