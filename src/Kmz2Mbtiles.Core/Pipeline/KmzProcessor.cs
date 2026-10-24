using System.Diagnostics;
using System.Globalization;
using Kmz2Mbtiles.Core.Abstractions;
using Kmz2Mbtiles.Core.Geo;
using Kmz2Mbtiles.Core.Kmz;
using Kmz2Mbtiles.Core.Mbtiles;
using Kmz2Mbtiles.Core.Naming;

namespace Kmz2Mbtiles.Core.Pipeline;

public sealed class KmzProcessor
{
    private readonly IGdalRunner _gdal;
    private readonly IProgressReporter _reporter;

    public KmzProcessor(IGdalRunner gdal, IProgressReporter reporter)
    {
        _gdal = gdal;
        _reporter = reporter;
    }

    public ProcessResult Process(string kmzPath, string baseDir)
    {
        kmzPath = Path.GetFullPath(kmzPath);

        if (!File.Exists(kmzPath))
        {
            _reporter.Warn($"SKIP not a file: {kmzPath}");
            return new ProcessResult
            {
                Success = false,
                KmzPath = kmzPath,
                ErrorMessage = "not a file",
            };
        }
        if (!kmzPath.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase))
        {
            _reporter.Warn($"SKIP not .kmz: {kmzPath}");
            return new ProcessResult
            {
                Success = false,
                KmzPath = kmzPath,
                ErrorMessage = "not .kmz",
            };
        }

        var normDir = Path.Combine(baseDir, "normalized");
        var mbtilesDir = Path.Combine(baseDir, "mbtiles");

        _reporter.Header($"Processing: {Path.GetFileName(kmzPath)}");

        var tmp = Path.Combine(Path.GetTempPath(), "kmz2mbt_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        var sw = Stopwatch.StartNew();

        try
        {
            _reporter.Step(1, 5, "Extracting KMZ...");
            ZipHelper.Extract(kmzPath, tmp);

            var filesDir = Path.Combine(tmp, "files");
            if (!Directory.Exists(filesDir))
                throw new InvalidOperationException("no files/ folder");

            var jpgs = Directory.GetFiles(filesDir, "*.jpg");
            if (jpgs.Length != 1)
                throw new InvalidOperationException($"expected 1 jpg in files/, found {jpgs.Length}");

            var jpg = jpgs[0];
            var masterRaw = Path.GetFileNameWithoutExtension(jpg);
            var master = NameHelper.Normalize(masterRaw);
            _reporter.Step(2, 5, $"Master name: {masterRaw} -> {master}");

            _reporter.Step(3, 5, "Normalizing...");
            var kmlPath = Path.Combine(tmp, "doc.kml");
            if (!File.Exists(kmlPath))
                throw new InvalidOperationException("no doc.kml");

            KmlHelper.PatchNameAndHref(kmlPath, master);

            var kmzBase = Path.GetFileNameWithoutExtension(kmzPath);
            string? normalizedKmzPath = null;
            if (kmzBase == master)
            {
                _reporter.Info("already normalized, skipping");
            }
            else
            {
                Directory.CreateDirectory(normDir);
                normalizedKmzPath = Path.Combine(normDir, master + ".kmz");
                ZipHelper.Build(tmp, normalizedKmzPath);
                _reporter.Warn($"created normalized/{master}.kmz");
            }

            _reporter.Step(4, 5, "Creating GCP-TIFF...");
            var (W, H) = _gdal.GetJpgSize(jpg);
            var corners = KmlHelper.ParseLatLonQuad(kmlPath);

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
            args.Add(jpg);
            args.Add(tifGcp);

            if (!_gdal.Run("gdal_translate", "gdal_translate (GCP-TIFF, 4 points)", args.ToArray()))
                throw new InvalidOperationException("gdal_translate (GCP-TIFF) failed");

            _reporter.Step(5, 5, "Converting to MBTiles...");
            var tif3857 = Path.Combine(tmp, master + "_temp_3857.tif");

            if (!_gdal.Run("gdalwarp", "gdalwarp (EPSG:3857, cubic)",
                    "-t_srs", "EPSG:3857", "-r", "cubic",
                    "-co", "TILED=YES", "-co", "COMPRESS=LZW",
                    tifGcp, tif3857))
                throw new InvalidOperationException("gdalwarp failed");

            Directory.CreateDirectory(mbtilesDir);
            var outMbtiles = Path.Combine(mbtilesDir, master + ".mbtiles");

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
                // если не удалось прочитать статистику — не критично
            }

            _reporter.Success(Path.GetFileName(outMbtiles));
            if (normalizedKmzPath != null)
                _reporter.Path("normalized", normalizedKmzPath);
            _reporter.Path("mbtiles", outMbtiles);
            _reporter.Detail($"size  : {sizeBytes / 1048576.0:F1} MB");
            if (tileCount > 0)
                _reporter.Detail($"tiles : {tileCount} (z{minZ}-z{maxZ})");
            _reporter.Detail($"time  : {FormatElapsed(sw.Elapsed)}");

            return new ProcessResult
            {
                Success = true,
                KmzPath = kmzPath,
                MbtilesPath = outMbtiles,
                NormalizedKmzPath = normalizedKmzPath,
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
            _reporter.Error($"{Path.GetFileName(kmzPath)}: {ex.Message}");
            return new ProcessResult
            {
                Success = false,
                KmzPath = kmzPath,
                ErrorMessage = ex.Message,
                Elapsed = sw.Elapsed,
            };
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { }
        }
    }

    private static string FormatElapsed(TimeSpan t)
    {
        if (t.TotalMinutes >= 1)
            return $"{(int)t.TotalMinutes} min {t.Seconds} sec";
        return $"{t.TotalSeconds:F1} sec";
    }
}