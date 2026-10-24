using Kmz2Mbtiles.Core.Pipeline;

namespace Kmz2Mbtiles.Cli;

/// <summary>
/// Command-line entry point that drives the conversion pipeline for each input file.
/// </summary>
/// <param name="processor">Pipeline processor used for every input KMZ.</param>
/// <remarks>
/// The application accepts one or more KMZ paths as command-line arguments. Each path is
/// processed independently, and the output is written next to the input file, inside
/// <c>normalized</c> and <c>mbtiles</c> subdirectories. The exit code reflects whether
/// every input was processed successfully.
/// </remarks>
public sealed class CliApp(KmzProcessor processor)
{
    private readonly KmzProcessor _processor = processor;

    /// <summary>
    /// Runs the application for the given command-line arguments.
    /// </summary>
    /// <param name="args">
    /// Command-line arguments. Each element is treated as a path to a KMZ file; surrounding
    /// double quotes are removed.
    /// </param>
    /// <returns>
    /// <c>0</c> if every input was processed successfully; <c>1</c> if no arguments were
    /// provided or at least one input failed.
    /// </returns>
    /// <remarks>
    /// Failures are reported through the progress reporter by the pipeline itself; this
    /// method only aggregates the overall outcome into a process exit code.
    /// </remarks>
    public int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: drag and drop a .kmz file onto Kmz2Mbtiles.bat");
            return 1;
        }

        var anyFailed = false;

        foreach (var arg in args)
        {
            var path = arg.Trim('"');
            var fullPath = Path.GetFullPath(path);
            var baseDir = Path.GetDirectoryName(fullPath)!;
            var result = _processor.Process(fullPath, baseDir);
            if (!result.Success)
                anyFailed = true;
        }

        return anyFailed ? 1 : 0;
    }
}