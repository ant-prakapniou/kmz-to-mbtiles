using Microsoft.Data.Sqlite;

namespace Kmz2Mbtiles.Core.Mbtiles;

/// <summary>
/// Reads summary information from MBTiles files.
/// </summary>
public static class MbtilesInfo
{
    /// <summary>
    /// Reads tile count and zoom level range from the <c>tiles</c> table of an MBTiles file.
    /// </summary>
    /// <param name="mbtilesPath">Path to the MBTiles file to inspect.</param>
    /// <returns>
    /// A tuple containing the total number of tiles, the minimum zoom level and the maximum
    /// zoom level present in the file. If the <c>tiles</c> table is empty, all values are 0.
    /// </returns>
    /// <remarks>
    /// The database is opened in read-only mode. Missing zoom values are reported as 0 rather
    /// than causing an exception. Callers are expected to treat a count of 0 as "statistics
    /// unavailable" and continue; failure to read statistics is not considered fatal for
    /// the conversion pipeline.
    /// </remarks>
    public static (long Count, int MinZoom, int MaxZoom) GetStats(string mbtilesPath)
    {
        var connStr = new SqliteConnectionStringBuilder
        {
            DataSource = mbtilesPath,
            Mode = SqliteOpenMode.ReadOnly,
        }.ToString();

        using var conn = new SqliteConnection(connStr);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*), MIN(zoom_level), MAX(zoom_level) FROM tiles";
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            var count = reader.GetInt64(0);
            var minZ = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var maxZ = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            return (count, minZ, maxZ);
        }
        return (0, 0, 0);
    }
}