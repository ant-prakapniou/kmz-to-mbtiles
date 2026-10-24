using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Kmz2Mbtiles.Core.Abstractions;

namespace Kmz2Mbtiles.Core.Geo;

/// <summary>
/// Default <see cref="IGdalRunner"/> implementation that invokes GDAL command-line tools
/// as child processes and reports their outcome through an <see cref="IProgressReporter"/>.
/// </summary>
/// <param name="reporter">Progress reporter used to surface command execution status.</param>
/// <remarks>
/// GDAL executables (<c>gdal_translate</c>, <c>gdalwarp</c>, <c>gdaladdo</c>, <c>gdalinfo</c>)
/// are expected to be available on the system <c>PATH</c>. No executable discovery is performed.
/// </remarks>
public sealed class GdalRunner(IProgressReporter reporter) : IGdalRunner
{
    private readonly IProgressReporter _reporter = reporter;

    /// <inheritdoc/>
    /// <remarks>
    /// Standard output is consumed but discarded. Standard error is captured and, on failure,
    /// forwarded to the reporter line by line so the caller can diagnose the problem.
    /// </remarks>
    public bool Run(string exe, string label, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var a in args)
            psi.ArgumentList.Add(a);

        _reporter.Detail($"> {label}");

        using var p = new Process { StartInfo = psi };
        var stderr = new StringBuilder();

        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stderr.AppendLine(e.Data);
        };

        p.Start();
        p.BeginErrorReadLine();

        p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        if (p.ExitCode == 0)
        {
            _reporter.Success("OK");
            return true;
        }

        _reporter.Error($"FAIL {exe} {string.Join(' ', args)}");
        if (stderr.Length > 0)
        {
            _reporter.Error("--- stderr ---");
            foreach (var line in stderr.ToString().Split('\n'))
                if (!string.IsNullOrWhiteSpace(line))
                    _reporter.Error(line.TrimEnd());
            _reporter.Error("---------------");
        }
        return false;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The image dimensions are extracted from the <c>Size is W, H</c> line printed by
    /// <c>gdalinfo</c>. If that line is missing, the method throws rather than returning
    /// a sentinel value, because a missing size indicates a corrupted or unsupported input.
    /// </remarks>
    public (int W, int H) GetJpgSize(string jpgPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "gdalinfo",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add(jpgPath);

        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        var m = Regex.Match(output, @"Size is (\d+),\s*(\d+)");
        if (!m.Success)
            throw new InvalidOperationException("failed to determine jpg size");

        return (int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
    }
}