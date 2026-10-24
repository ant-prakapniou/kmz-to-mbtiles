namespace Kmz2Mbtiles.Core.Abstractions;

/// <summary>
/// Result of processing a single KMZ file through the conversion pipeline.
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
    /// Absolute path to the input KMZ file that was processed.
    /// </summary>
    public required string KmzPath { get; init; }

    /// <summary>
    /// Error message describing why processing failed.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="false"/>.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Absolute path to the generated MBTiles file.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>.
    /// </remarks>
    public string? MbtilesPath { get; init; }

    /// <summary>
    /// Absolute path to the normalized KMZ file written to the <c>normalized</c> directory.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/> and the input
    /// required normalization. If the input KMZ already carried the normalized master name,
    /// no file is written and this property is <see langword="null"/>.
    /// </remarks>
    public string? NormalizedKmzPath { get; init; }

    /// <summary>
    /// Size of the generated MBTiles file, in bytes.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>.
    /// </remarks>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Total number of tiles in the generated MBTiles file, across all zoom levels.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>.
    /// A value of 0 indicates that the statistics could not be read; this is
    /// treated as non-fatal and does not affect <see cref="Success"/>.
    /// </remarks>
    public long TileCount { get; init; }

    /// <summary>
    /// Minimum zoom level present in the generated MBTiles file.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>
    /// and <see cref="TileCount"/> is greater than 0.
    /// </remarks>
    public int MinZoom { get; init; }

    /// <summary>
    /// Maximum zoom level present in the generated MBTiles file.
    /// </summary>
    /// <remarks>
    /// Populated only when <see cref="Success"/> is <see langword="true"/>
    /// and <see cref="TileCount"/> is greater than 0.
    /// </remarks>
    public int MaxZoom { get; init; }

    /// <summary>
    /// Wall-clock time spent processing the input KMZ file.
    /// </summary>
    public TimeSpan Elapsed { get; init; }
}