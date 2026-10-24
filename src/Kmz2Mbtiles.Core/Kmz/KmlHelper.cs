using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Kmz2Mbtiles.Core.Kmz;

/// <summary>
/// Reads and patches KML documents embedded in KMZ archives.
/// </summary>
public static class KmlHelper
{
    private static readonly XNamespace GxNs = "http://www.google.com/kml/ext/2.2";

    /// <summary>
    /// Parses the four corner coordinates from a <c>&lt;gx:LatLonQuad&gt;</c> element.
    /// </summary>
    /// <param name="kmlPath">Path to the KML file to read.</param>
    /// <returns>
    /// An array of four <c>(Lon, Lat)</c> tuples in the order they appear in the KML document,
    /// which is south-west, south-east, north-east, north-west.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <c>&lt;gx:LatLonQuad&gt;</c> element is present, or when the number of
    /// parsed coordinates is not exactly four.
    /// </exception>
    /// <remarks>
    /// The order of the returned corners matches the Google Earth convention and is relied upon
    /// by the pipeline when assigning ground control points to the raster. Changing this order
    /// would flip or rotate the resulting image on the map.
    /// </remarks>
    public static (double Lon, double Lat)[] ParseLatLonQuad(string kmlPath)
    {
        var doc = XDocument.Load(kmlPath);
        var coords = doc.Descendants(GxNs + "LatLonQuad")
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "coordinates");

        if (coords == null)
            throw new InvalidOperationException("no <gx:LatLonQuad> element found in doc.kml");

        var tokens = coords.Value
            .Split([' ', '\t', '\n', '\r'],
                   StringSplitOptions.RemoveEmptyEntries);

        var result = new List<(double, double)>();
        foreach (var token in tokens)
        {
            var parts = token.Split(',');
            if (parts.Length < 2)
                continue;
            var lon = double.Parse(parts[0], CultureInfo.InvariantCulture);
            var lat = double.Parse(parts[1], CultureInfo.InvariantCulture);
            result.Add((lon, lat));
        }

        if (result.Count != 4)
            throw new InvalidOperationException(
                $"expected 4 corners in LatLonQuad, found {result.Count}");

        return [.. result];
    }

    /// <summary>
    /// Replaces the content of the first <c>&lt;name&gt;</c> and the first <c>&lt;href&gt;</c>
    /// elements with values derived from the given master name.
    /// </summary>
    /// <param name="kmlPath">Path to the KML file to patch in place.</param>
    /// <param name="master">Normalized master name to write into <c>&lt;name&gt;</c> and <c>&lt;href&gt;</c>.</param>
    /// <remarks>
    /// The document is written back as UTF-8 without a byte order mark. This preserves the
    /// original file layout and avoids introducing a BOM that some consumers of the KMZ
    /// archive treat as part of the content.
    /// </remarks>
    public static void PatchNameAndHref(string kmlPath, string master)
    {
        var text = File.ReadAllText(kmlPath, Encoding.UTF8);

        text = Regex.Replace(
            text,
            @"(?s)<name>.*?</name>",
            $"<name>{master}</name>",
            RegexOptions.None,
            TimeSpan.FromSeconds(5));

        // Regex.Replace does not support limiting the number of replacements,
        // so the first <name> and <href> are replaced manually below.
        text = ReplaceFirst(text, @"(?s)<name>.*?</name>", $"<name>{master}</name>");
        text = ReplaceFirst(text, @"(?s)<href>.*?</href>", $"<href>files/{master}.jpg</href>");

        File.WriteAllText(kmlPath, text, new UTF8Encoding(false));
    }

    /// <summary>
    /// Replaces the first occurrence of <paramref name="pattern"/> in <paramref name="input"/>.
    /// </summary>
    /// <param name="input">Source string.</param>
    /// <param name="pattern">Regular expression pattern to search for.</param>
    /// <param name="replacement">Replacement string.</param>
    /// <returns>
    /// The input string with the first match replaced, or the original string if no match is found.
    /// </returns>
    private static string ReplaceFirst(string input, string pattern, string replacement)
    {
        var match = Regex.Match(input, pattern);
        if (!match.Success)
            return input;
        return input.Substring(0, match.Index)
             + replacement
             + input.Substring(match.Index + match.Length);
    }
}