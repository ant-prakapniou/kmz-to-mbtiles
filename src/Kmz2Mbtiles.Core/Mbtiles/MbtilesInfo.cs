using Microsoft.Data.Sqlite;

namespace Kmz2Mbtiles.Core.Mbtiles;

public static class MbtilesInfo
{
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