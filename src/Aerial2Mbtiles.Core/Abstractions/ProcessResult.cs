namespace Aerial2Mbtiles.Core.Abstractions;

/// <summary>
/// Kind of input supplied to the pipeline.
/// </summary>
public enum InputKind
{
    /// <summary>
    /// A single KMZ archive containing a KML document and one raster asset.
    /// </summary>
    Kmz,

    /// <summary>
    /// A pair of files: one raster image and one KML document lying side by side.
    /// </summary>
    Pair,
}

/// <summary>
/// Result of processing a single input (KMZ archive or raster+KML pair) through
/// the conversion pipeline.
/// </summary>
/// <remarks>
/// A single instance describes both successful and failed runs. Callers should
/// inspect <see cref="Success"/> first; the remaining properties are only
/// meaningful in the corresponding case. On success, output paths and metrics
/// are populated. On failure, <see cref="ErrorMessage"/> carries the reason.
/// </remarks>
public sealed record ProcessResult
{
    /// <summary>
    /// Indicates whether the pipeline completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Absolute path to the input that was processed: either the KMZ archive
    /// or the raster image of a pair.
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Kind of input that was processed.
    /// </summary>
    public required InputKind InputKind { get; init; }

    /// <summary>
    /// Processing mode requested for this run: <c>1</c> for normalization only,
    /// <c>2</c> for normalization plus MBTiles conversion.
    /// </summary>
    public required int Mode { get; init; }

    /// <summary>
    /// Error message describing why processing failed.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="false"/>.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Normalized master name used for all output files.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>.
    /// </remarks>
    public string? Master { get; init; }

    /// <summary>
    /// Absolute path to the output directory <c>{master}</c> that holds all
    /// artifacts produced by this run.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>.
    /// </remarks>
    public string? OutputDir { get; init; }

    /// <summary>
    /// Absolute path to the normalized raster file <c>{master}{ext}</c>
    /// inside <see cref="OutputDir"/>.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>.
    /// </remarks>
    public string? RasterPath { get; init; }

    /// <summary>
    /// Absolute path to the normalized KML file <c>{master}.kml</c>
    /// inside <see cref="OutputDir"/>.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>.
    /// </remarks>
    public string? KmlPath { get; init; }

    /// <summary>
    /// Absolute path to the generated MBTiles file <c>{master}.mbtiles</c>
    /// inside <see cref="OutputDir"/>.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>
    /// and <see cref="Mode"/> is <c>2</c>. Always <see langword="null"/> in mode 1.
    /// </remarks>
    public string? MbtilesPath { get; init; }

    /// <summary>
    /// Size of the generated MBTiles file, in bytes.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>
    /// and <see cref="Mode"/> is <c>2</c>.
    /// </remarks>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Total number of tiles in the generated MBTiles file, across all zoom levels.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>
    /// and <see cref="Mode"/> is <c>2</c>. A value of 0 indicates that the
    /// statistics could not be read; this is treated as non-fatal and does not
    /// affect <see cref="Success"/>.
    /// </remarks>
    public long TileCount { get; init; }

    /// <summary>
    /// Minimum zoom level present in the generated MBTiles file.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>,
    /// <see cref="Mode"/> is <c>2</c>, and <see cref="TileCount"/> is greater than 0.
    /// </remarks>
    public int MinZoom { get; init; }

    /// <summary>
    /// Maximum zoom level present in the generated MBTiles file.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>,
    /// <see cref="Mode"/> is <c>2</c>, and <see cref="TileCount"/> is greater than 0.
    /// </remarks>
    public int MaxZoom { get; init; }

    /// <summary>
    /// Wall-clock time spent processing the input.
    /// </summary>
    public TimeSpan Elapsed { get; init; }
}