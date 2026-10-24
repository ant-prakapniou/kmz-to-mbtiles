namespace Kmz2Mbtiles.Core.Abstractions;

/// <summary>
/// Abstraction over the GDAL command-line tools used by the conversion pipeline.
/// </summary>
/// <remarks>
/// The pipeline relies on external GDAL executables (<c>gdal_translate</c>, <c>gdalwarp</c>,
/// <c>gdaladdo</c>, <c>gdalinfo</c>) that must be available on the system <c>PATH</c>.
/// This interface exists to isolate that external dependency so the pipeline can be
/// exercised in tests without invoking real GDAL binaries.
/// </remarks>
public interface IGdalRunner
{
    /// <summary>
    /// Runs a GDAL executable synchronously and reports the result through the progress reporter.
    /// </summary>
    /// <param name="exe">Executable name or path (for example, <c>gdal_translate</c>).</param>
    /// <param name="label">Human-readable label describing the operation, shown before execution.</param>
    /// <param name="args">Command-line arguments passed verbatim to the executable.</param>
    /// <returns><see langword="true"/> if the process exited with code 0; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// On success, only a short status line is reported. On failure, the full command line
    /// and captured <c>stderr</c> output are reported to aid diagnosis.
    /// </remarks>
    bool Run(string exe, string label, params string[] args);

    /// <summary>
    /// Reads the pixel dimensions of a raster image using <c>gdalinfo</c>.
    /// </summary>
    /// <param name="jpgPath">Path to the raster file to inspect.</param>
    /// <returns>A tuple containing the width and height of the image in pixels.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>gdalinfo</c> output does not contain a parseable <c>Size is W, H</c> line.
    /// </exception>
    (int W, int H) GetJpgSize(string jpgPath);
}