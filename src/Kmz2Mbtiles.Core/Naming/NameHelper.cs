using System.Text.RegularExpressions;
using NickBuhro.Translit;

namespace Kmz2Mbtiles.Core.Naming;

/// <summary>
/// Produces safe, normalized file names from raw master names taken from archived
/// aerial photographs.
/// </summary>
/// <remarks>
/// Master names originate from the archive and are considered the single source of truth.
/// They may contain Cyrillic characters, spaces and typographic symbols that are unsafe or
/// inconvenient as file names. This class transliterates Cyrillic to Latin and sanitizes the
/// result so the same name can be used for the KMZ archive, the JPG asset, the KML
/// <c>&lt;name&gt;</c> and <c>&lt;href&gt;</c> elements, and the output MBTiles file.
/// </remarks>
public static class NameHelper
{
    private static readonly Regex CyrillicRegex = new(@"\p{IsCyrillic}", RegexOptions.Compiled);

    /// <summary>
    /// Characters that are technically valid in Windows file names but may cause problems
    /// in Google Drive, cloud storage or older file systems. Replaced with <c>_</c>.
    /// </summary>
    private static readonly char[] ExtraUnsafeChars =
    {
        '«', '»', '№', '“', '”', '„', '‘', '’', '–', '—', '…', '·',
    };

    /// <summary>
    /// Determines whether the string contains at least one Cyrillic character.
    /// </summary>
    /// <param name="s">String to inspect.</param>
    /// <returns><see langword="true"/> if at least one Cyrillic character is present; otherwise <see langword="false"/>.</returns>
    public static bool ContainsCyrillic(string s) => CyrillicRegex.IsMatch(s);

    /// <summary>
    /// Normalizes a master name: transliterates Cyrillic to Latin, then sanitizes the result.
    /// </summary>
    /// <param name="master">Raw master name, typically the original JPG filename without extension.</param>
    /// <returns>A name suitable for use as a file name and as a value in KML <c>&lt;name&gt;</c> and <c>&lt;href&gt;</c> elements.</returns>
    /// <remarks>
    /// Transliteration follows GOST 7.79-2000 (ISO 9, system B) and is applied only when the
    /// input contains Cyrillic characters. Sanitization is always applied, including for
    /// already Latin names, to keep the output uniform.
    /// </remarks>
    /// <example>
    /// <code>
    /// NameHelper.Normalize("222-55 Киселева буда выс 43.10.21");
    /// // returns "222-55_Kiseleva_buda_vys_43.10.21"
    /// </code>
    /// </example>
    public static string Normalize(string master)
    {
        var s = master;

        if (ContainsCyrillic(s))
        {
            s = Transliteration.CyrillicToLatin(s, Language.Russian);
        }

        s = Sanitize(s);
        return s;
    }

    /// <summary>
    /// Sanitizes a string so it can be used as a file name.
    /// </summary>
    /// <param name="s">Input string.</param>
    /// <returns>A file-name-safe version of the input.</returns>
    /// <remarks>
    /// The following transformations are applied, in order:
    /// <list type="number">
    ///   <item><description>Spaces are replaced with <c>_</c>.</description></item>
    ///   <item><description>Quotes and backticks left over from transliteration are removed.</description></item>
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

        // Remove leftovers from transliteration of ъ, ь, ы (quotes and backtick)
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