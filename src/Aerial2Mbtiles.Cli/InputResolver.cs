using Aerial2Mbtiles.Core.Abstractions;
using Aerial2Mbtiles.Core.Helpers;

namespace Aerial2Mbtiles.Cli;

/// <summary>
/// Resolves command-line arguments into a concrete raster + KML pair.
/// </summary>
/// <remarks>
/// Supports two input shapes: a single <c>.kmz</c> archive, or a pair of files
/// (a raster image and a <c>.kml</c>) passed in any order. A KMZ archive is
/// unpacked into a temporary directory using the same entry-name encoding as
/// Google Earth (code page 437 with UTF-8 override); the caller is responsible
/// for disposing the returned <see cref="ResolvedInput"/> so that temporary
/// files are removed.
/// </remarks>
public static class InputResolver
{
    private static readonly HashSet<string> RasterExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".tif", ".tiff", ".png" };

    /// <summary>
    /// Tries to resolve the given arguments into a raster + KML pair.
    /// </summary>
    public static bool TryResolve(
        string[] args,
        out ResolvedInput? input,
        out string error)
    {
        input = null;
        error = string.Empty;

        if (args.Length == 0)
        {
            error = "no input";
            return false;
        }

        // One argument: must be .kmz.
        if (args.Length == 1)
        {
            var path = Path.GetFullPath(args[0].Trim('"'));
            if (!path.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase))
            {
                error = "single input must be a .kmz file";
                return false;
            }
            if (!File.Exists(path))
            {
                error = $"file not found: {path}";
                return false;
            }
            return TryResolveKmz(path, out input, out error);
        }

        // Two arguments: raster + .kml in any order.
        if (args.Length == 2)
        {
            string? raster = null, kml = null;
            foreach (var raw in args)
            {
                var path = Path.GetFullPath(raw.Trim('"'));
                if (path.EndsWith(".kml", StringComparison.OrdinalIgnoreCase) && kml is null)
                    kml = path;
                else if (RasterExtensions.Contains(Path.GetExtension(path)) && raster is null)
                    raster = path;
                else
                {
                    error = $"unexpected argument: {path}";
                    return false;
                }
            }
            if (raster is null || kml is null)
            {
                error = "expected a raster image and a .kml file";
                return false;
            }
            if (!File.Exists(raster)) { error = $"file not found: {raster}"; return false; }
            if (!File.Exists(kml)) { error = $"file not found: {kml}"; return false; }

            input = new ResolvedInput(
                InputPath: raster,
                InputKind: InputKind.Pair,
                RasterPath: raster,
                KmlPath: kml,
                TempDir: null);
            return true;
        }

        error = "expected either one .kmz file or a raster + .kml pair";
        return false;
    }

    private static bool TryResolveKmz(
        string kmzPath,
        out ResolvedInput? input,
        out string error)
    {
        input = null;
        error = string.Empty;

        var tmp = Path.Combine(Path.GetTempPath(), "aerial2mbt_in_" + Guid.NewGuid().ToString("N"));

        try
        {
            ZipHelper.Extract(kmzPath, tmp);

            var filesDir = Path.Combine(tmp, "files");
            if (!Directory.Exists(filesDir))
            {
                error = "KMZ has no files/ folder";
                Cleanup(tmp);
                return false;
            }

            var rasters = Directory.GetFiles(filesDir)
                .Where(p => RasterExtensions.Contains(Path.GetExtension(p)))
                .ToArray();

            if (rasters.Length != 1)
            {
                error = $"expected 1 raster in files/, found {rasters.Length}";
                Cleanup(tmp);
                return false;
            }

            var kml = Path.Combine(tmp, "doc.kml");
            if (!File.Exists(kml))
            {
                error = "KMZ has no doc.kml";
                Cleanup(tmp);
                return false;
            }

            input = new ResolvedInput(
                InputPath: kmzPath,
                InputKind: InputKind.Kmz,
                RasterPath: rasters[0],
                KmlPath: kml,
                TempDir: tmp);
            return true;
        }
        catch (Exception ex)
        {
            error = $"failed to unpack KMZ: {ex.Message}";
            Cleanup(tmp);
            return false;
        }
    }

    private static void Cleanup(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }
}

/// <summary>
/// Concrete raster + KML pair resolved from the command-line arguments.
/// </summary>
/// <param name="InputPath">Original input path (KMZ or raster), for reporting.</param>
/// <param name="InputKind">Kind of the original input.</param>
/// <param name="RasterPath">Absolute path to the raster file.</param>
/// <param name="KmlPath">Absolute path to the KML file.</param>
/// <param name="TempDir">
/// Temporary directory holding the unpacked KMZ, or <see langword="null"/> for a pair.
/// The caller must remove it after processing.
/// </param>
public sealed record ResolvedInput(
    string InputPath,
    InputKind InputKind,
    string RasterPath,
    string KmlPath,
    string? TempDir) : IDisposable
{
    /// <inheritdoc/>
    public void Dispose()
    {
        if (TempDir is not null)
            try { Directory.Delete(TempDir, recursive: true); } catch { }
    }
}