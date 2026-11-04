namespace Aerial2Mbtiles.Core.Helpers;

/// <summary>
/// Produces safe file names from raw master names taken from archived aerial photographs.
/// </summary>
/// <remarks>
/// Master names originate from the archive and are considered the single source of truth.
/// They may contain spaces and typographic symbols that are unsafe or inconvenient as
/// file names. This class sanitizes such names so the same value can be used for the
/// raster file, the KML file, the KML <c>&lt;name&gt;</c> and <c>&lt;href&gt;</c> elements,
/// and the output MBTiles file.
/// </remarks>
public static class NameHelper
{
    /// <summary>
    /// Characters that are technically valid in Windows file names but may cause problems
    /// in Google Drive, cloud storage or older file systems. Replaced with <c>_</c>.
    /// </summary>
    private static readonly char[] ExtraUnsafeChars =
    {
        '«', '»', '№', '“', '”', '„', '‘', '’', '–', '—', '…', '·',
    };

    /// <summary>
    /// Sanitizes a string so it can be used as a file name.
    /// </summary>
    /// <param name="s">Input string.</param>
    /// <returns>A file-name-safe version of the input.</returns>
    /// <remarks>
    /// The following transformations are applied, in order:
    /// <list type="number">
    ///   <item><description>Spaces are replaced with <c>_</c>.</description></item>
    ///   <item><description>Quotes and backticks are removed.</description></item>
    ///   <item><description>File-system invalid characters and a fixed set of typographic symbols are replaced with <c>_</c>.</description></item>
    ///   <item><description>Consecutive underscores are collapsed into one.</description></item>
    ///   <item><description>Trailing dots and spaces are trimmed.</description></item>
    ///   <item><description>Names longer than 200 characters are truncated, leaving headroom for the file extension.</description></item>
    /// </list>
    /// </remarks>
    public static string Sanitize(string s)
    {
        // Spaces -> underscores
        s = s.Replace(' ', '_');

        // Remove quotes and backticks
        s = s.Replace("\"", "").Replace("'", "").Replace("`", "");

        // Invalid file name characters -> underscore
        var invalid = Path.GetInvalidFileNameChars();
        var unsafeSet = new HashSet<char>(invalid);
        foreach (var c in ExtraUnsafeChars)
            unsafeSet.Add(c);

        var chars = s.Select(c => unsafeSet.Contains(c) ? '_' : c).ToArray();
        s = new string(chars);

        // Collapse consecutive underscores
        while (s.Contains("__"))
            s = s.Replace("__", "_");

        // Trim trailing dots and spaces
        s = s.TrimEnd('.', ' ');

        // Length limit (255 minus headroom for the extension)
        if (s.Length > 200)
            s = s.Substring(0, 200).TrimEnd('_', '.', ' ');

        return s;
    }
}