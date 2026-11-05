using Aerial2Mbtiles.Core.Abstractions;
using Aerial2Mbtiles.Core.Helpers;
using Aerial2Mbtiles.Core.Pipeline;

namespace Aerial2Mbtiles.Cli;

/// <summary>
/// Command-line entry point that drives the conversion pipeline for a single input.
/// </summary>
/// <param name="processor">Pipeline processor used for the accepted input.</param>
/// <remarks>
/// The application accepts either one <c>.kmz</c> archive or a pair of files
/// (a raster image and a <c>.kml</c> document). After the input has been resolved,
/// the user is asked for a normalized master name (pre-filled with the raster file
/// name) and for a processing mode: <c>1</c> for normalization only, <c>2</c> to
/// also produce MBTiles.
/// </remarks>
public sealed class CliApp(AerialProcessor processor, IProgressReporter reporter)
{
    private readonly AerialProcessor _processor = processor;
    private readonly IProgressReporter _reporter = reporter;

    public int Run(string[] args)
    {
        if (!InputResolver.TryResolve(args, out var input, out var error))
        {
            Console.Error.WriteLine(error);
            PrintUsage();
            return 1;
        }

        using var resolved = input!;

        // ----- Show the 4 current names -----
        var rasterBase = Path.GetFileNameWithoutExtension(resolved.RasterPath);
        var kmlBase = Path.GetFileNameWithoutExtension(resolved.KmlPath);
        var (kmlName, kmlHref) = KmlHelper.ReadNames(resolved.KmlPath);

        _reporter.Header("Current names");
        _reporter.Detail($"  raster : {rasterBase}");
        _reporter.Detail($"  kml    : {kmlBase}");
        _reporter.Detail($"  <name> : {kmlName ?? "(missing)"}");
        _reporter.Detail($"  <href> : {kmlHref ?? "(missing)"}");
        _reporter.Detail("");

        // ----- Master name (pre-filled with the raster base name) -----
        var suggested = NameHelper.Normalize(rasterBase);

        _reporter.Info($"Master name [{suggested}]. Press Enter to accept.");
        Console.Write("> ");
        var rawName = Console.ReadLine();
        var master = NameHelper.Normalize(
            string.IsNullOrWhiteSpace(rawName) ? suggested : rawName.Trim());

        if (string.IsNullOrWhiteSpace(master))
        {
            Console.Error.WriteLine("Master name is empty after normalization.");
            return 1;
        }

        // ----- Processing mode -----
        _reporter.Info("Mode (1 = normalize only, 2 = normalize + MBTiles) [2]. Press Enter to accept.");
        Console.Write("> ");
        var rawMode = Console.ReadLine();
        var mode = string.IsNullOrWhiteSpace(rawMode)
            ? 2
            : int.TryParse(rawMode.Trim(), out var m) ? m : -1;

        if (mode is not (1 or 2))
        {
            Console.Error.WriteLine("Mode must be 1 or 2.");
            return 1;
        }

        // ----- Run the pipeline -----
        var baseDir = Path.GetDirectoryName(resolved.InputPath)!;

        var result = _processor.Process(
            resolved.InputPath,
            resolved.InputKind,
            resolved.RasterPath,
            resolved.KmlPath,
            master,
            mode,
            baseDir);

        return result.Success ? 0 : 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  drag and drop a .kmz file onto Aerial2Mbtiles.bat");
        Console.WriteLine("  drag and drop a raster + .kml pair onto Aerial2Mbtiles.bat");
    }
}