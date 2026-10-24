namespace Kmz2Mbtiles.Core.Abstractions;

public sealed record ProcessResult
{
    public required bool Success { get; init; }
    public required string KmzPath { get; init; }
    public string? ErrorMessage { get; init; }
    public string? MbtilesPath { get; init; }
    public string? NormalizedKmzPath { get; init; }
    public long SizeBytes { get; init; }
    public long TileCount { get; init; }
    public int MinZoom { get; init; }
    public int MaxZoom { get; init; }
    public TimeSpan Elapsed { get; init; }
}