using System.Diagnostics;
using System.Globalization;
using Aerial2Mbtiles.Core.Abstractions;
using Aerial2Mbtiles.Core.Helpers;
using Aerial2Mbtiles.Core.Mbtiles;

namespace Aerial2Mbtiles.Core.Pipeline;

/// <summary>
/// Orchestrates the normalization (and optional MBTiles conversion) of a single
/// raster + KML pair that has already been resolved by the caller.
/// </summary>
/// <param name="gdal">Runner used to invoke GDAL command-line tools.</param>
/// <param name="reporter">Progress reporter used to surface pipeline status and results.</param>
/// <remarks>
/// This processor never unpacks archives and never guesses input layout. It expects
/// a concrete raster file and a concrete KML file, both readable. Any unpacking of
/// a KMZ archive into such a pair is the caller's responsibility.
/// </remarks>
public sealed class AerialProcessor(IGdalRunner gdal, IProgressReporter reporter)
{
    private readonly IGdalRunner _gdal = gdal;
    private readonly IProgressReporter _reporter = reporter;

    /// <summary>
    /// Normalizes a raster + KML pair into a <c>{master}</c> directory and, in mode 2,
    /// also produces MBTiles.
    /// </summary>
    /// <param name="inputPath">Original input path (KMZ or raster), used only for reporting.</param>
    /// <param name="inputKind">Kind of the original input, used only for reporting.</param>
    /// <param name="rasterPath">Absolute path to the source raster.</param>
    /// <param name="kmlPath">Absolute path to the source KML.</param>
    /// <param name="master">Normalized master name chosen by the user, without extension.</param>
    /// <param name="mode">Processing mode: <c>1</c> for normalization only, <c>2</c> to also produce MBTiles.</param>
    /// <param name="baseDir">Directory under which the <c>{master}</c> output directory is created.</param>
    /// <returns>A <see cref="ProcessResult"/> describing the outcome.</returns>
    public ProcessResult Process(
        string inputPath,
        InputKind inputKind,
        string rasterPath,
        string kmlPath,
        string master,
        int mode,
        string baseDir)
    {
        if (mode is not (1 or 2))
            return Fail(inputPath, inputKind, mode, "mode must be 1 or 2");

        if (!File.Exists(rasterPath))
            return Fail(inputPath, inputKind, mode, $"raster not found: {rasterPath}");
        if (!File.Exists(kmlPath))
            return Fail(inputPath, inputKind, mode, $"KML not found: {kmlPath}");

        var displayName = Path.GetFileName(inputPath);
        _reporter.Header($"Processing: {displayName}");

        var outputDir = Path.Combine(baseDir, master);
        if (Directory.Exists(outputDir))
            return Fail(inputPath, inputKind, mode, $"output directory already exists: {outputDir}");

        var sw = Stopwatch.StartNew();
        string? tmp = null;

        try
        {
            var rasterExt = Path.GetExtension(rasterPath);
            _reporter.Step(1, mode == 2 ? 3 : 2, $"Master name: {master}");

            // ----- Parse binding from the source KML before touching output -----
            _reporter.Step(2, mode == 2 ? 3 : 2, "Normalizing...");
            var corners = KmlHelper.ParseBinding(kmlPath);

            Directory.CreateDirectory(outputDir);
            var outRaster = Path.Combine(outputDir, master + rasterExt);
            var outKml = Path.Combine(outputDir, master + ".kml");

            File.Copy(rasterPath, outRaster, overwrite: false);
            File.Copy(kmlPath, outKml, overwrite: false);
            KmlHelper.PatchKml(outKml, master, rasterExt, corners);

            // ----- Mode 1 stops here -----
            if (mode == 1)
            {
                sw.Stop();
                _reporter.Success($"normalized -> {master}/");
                _reporter.Path("raster", outRaster);
                _reporter.Path("kml", outKml);
                _reporter.Detail($"time  : {FormatElapsed(sw.Elapsed)}");

                return new ProcessResult
                {
                    Success = true,
                    InputPath = inputPath,
                    InputKind = inputKind,
                    Mode = mode,
                    Master = master,
                    OutputDir = outputDir,
                    RasterPath = outRaster,
                    KmlPath = outKml,
                    Elapsed = sw.Elapsed,
                };
            }

            // ----- Mode 2: GDAL chain -----
            _reporter.Step(3, 3, "Converting to MBTiles...");

            tmp = Path.Combine(Path.GetTempPath(), "aerial2mbt_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            var (W, H) = _gdal.GetRasterSize(outRaster);

            // Corner order in the KML is SW, SE, NE, NW.
            // GCPs are assigned clockwise starting from the top-left pixel of the raster.
            var gcp = new (int px, int py, double lon, double lat)[]
            {
                (0, 0, corners[3].Lon, corners[3].Lat),
                (W, 0, corners[2].Lon, corners[2].Lat),
                (W, H, corners[1].Lon, corners[1].Lat),
                (0, H, corners[0].Lon, corners[0].Lat),
            };

            var tifGcp = Path.Combine(tmp, master + ".tif");

            var args = new List<string> { "-of", "GTiff", "-a_srs", "EPSG:4326" };
            foreach (var (px, py, lon, lat) in gcp)
            {
                args.Add("-gcp");
                args.Add(px.ToString(CultureInfo.InvariantCulture));
                args.Add(py.ToString(CultureInfo.InvariantCulture));
                args.Add(lon.ToString("R", CultureInfo.InvariantCulture));
                args.Add(lat.ToString("R", CultureInfo.InvariantCulture));
            }
            args.Add(outRaster);
            args.Add(tifGcp);

            if (!_gdal.Run("gdal_translate", "gdal_translate (GCP-TIFF, 4 points)", args.ToArray()))
                throw new InvalidOperationException("gdal_translate (GCP-TIFF) failed");

            var tif3857 = Path.Combine(tmp, master + "_temp_3857.tif");

            if (!_gdal.Run("gdalwarp", "gdalwarp (EPSG:3857, cubic)",
                    "-t_srs", "EPSG:3857", "-r", "cubic",
                    "-co", "TILED=YES", "-co", "COMPRESS=LZW",
                    tifGcp, tif3857))
                throw new InvalidOperationException("gdalwarp failed");

            var outMbtiles = Path.Combine(outputDir, master + ".mbtiles");

            if (!_gdal.Run("gdal_translate", "gdal_translate (MBTILES, PNG)",
                    "-of", "MBTILES", "-co", "TILE_FORMAT=PNG",
                    tif3857, outMbtiles))
                throw new InvalidOperationException("gdal_translate (MBTILES) failed");

            if (!_gdal.Run("gdaladdo", "gdaladdo (cubic, 2-128)",
                    "-r", "cubic", outMbtiles,
                    "2", "4", "8", "16", "32", "64", "128"))
                throw new InvalidOperationException("gdaladdo failed");

            sw.Stop();

            var sizeBytes = new FileInfo(outMbtiles).Length;

            long tileCount = 0;
            int minZ = 0, maxZ = 0;
            try
            {
                (tileCount, minZ, maxZ) = MbtilesInfo.GetStats(outMbtiles);
            }
            catch
            {
                // Reading statistics is optional; failure must not fail the conversion.
            }

            _reporter.Success($"normalized + mbtiles -> {master}/");
            _reporter.Path("raster", outRaster);
            _reporter.Path("kml", outKml);
            _reporter.Path("mbtiles", outMbtiles);
            _reporter.Detail($"size  : {sizeBytes / 1048576.0:F1} MB");
            if (tileCount > 0)
                _reporter.Detail($"tiles : {tileCount} (z{minZ}-z{maxZ})");
            _reporter.Detail($"time  : {FormatElapsed(sw.Elapsed)}");

            return new ProcessResult
            {
                Success = true,
                InputPath = inputPath,
                InputKind = inputKind,
                Mode = mode,
                Master = master,
                OutputDir = outputDir,
                RasterPath = outRaster,
                KmlPath = outKml,
                MbtilesPath = outMbtiles,
                SizeBytes = sizeBytes,
                TileCount = tileCount,
                MinZoom = minZ,
                MaxZoom = maxZ,
                Elapsed = sw.Elapsed,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _reporter.Error($"{displayName}: {ex.Message}");
            return Fail(inputPath, inputKind, mode, ex.Message, sw.Elapsed);
        }
        finally
        {
            if (tmp is not null)
                try { Directory.Delete(tmp, true); } catch { }
        }
    }

    private ProcessResult Fail(
        string inputPath, InputKind kind, int mode, string message, TimeSpan elapsed = default)
    {
        _reporter.Warn($"SKIP: {message}");
        return new ProcessResult
        {
            Success = false,
            InputPath = inputPath,
            InputKind = kind,
            Mode = mode,
            ErrorMessage = message,
            Elapsed = elapsed,
        };
    }

    private static string FormatElapsed(TimeSpan t)
    {
        if (t.TotalMinutes >= 1)
            return $"{(int)t.TotalMinutes} min {t.Seconds} sec";
        return $"{t.TotalSeconds:F1} sec";
    }
}