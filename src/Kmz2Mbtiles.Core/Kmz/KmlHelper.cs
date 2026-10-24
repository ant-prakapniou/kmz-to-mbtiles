using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Kmz2Mbtiles.Core.Kmz;

public static class KmlHelper
{
    private static readonly XNamespace KmlNs = "http://www.opengis.net/kml/2.2";
    private static readonly XNamespace GxNs = "http://www.google.com/kml/ext/2.2";

    /// <summary>
    /// Парсит 4 угла из &lt;gx:LatLonQuad&gt;.
    /// Возвращает массив 4 кортежей (lon, lat) в порядке KML: ЮЗ, ЮВ, СВ, СЗ.
    /// </summary>
    public static (double Lon, double Lat)[] ParseLatLonQuad(string kmlPath)
    {
        var doc = XDocument.Load(kmlPath);
        var coords = doc.Descendants(GxNs + "LatLonQuad")
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "coordinates");

        if (coords == null)
            throw new InvalidOperationException("не найден <gx:LatLonQuad> в doc.kml");

        var tokens = coords.Value
            .Split(new[] { ' ', '\t', '\n', '\r' },
                   StringSplitOptions.RemoveEmptyEntries);

        var result = new List<(double, double)>();
        foreach (var token in tokens)
        {
            var parts = token.Split(',');
            if (parts.Length < 2)
                continue;
            var lon = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            var lat = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            result.Add((lon, lat));
        }

        if (result.Count != 4)
            throw new InvalidOperationException(
                $"ожидалось 4 угла в LatLonQuad, найдено {result.Count}");

        return result.ToArray();
    }

    /// <summary>
    /// Подменяет содержимое первого &lt;name&gt; и первого &lt;href&gt;.
    /// Сохраняет форматирование (UTF-8 без BOM), чтобы не сломать исходный файл.
    /// </summary>
    public static void PatchNameAndHref(string kmlPath, string master)
    {
        var text = File.ReadAllText(kmlPath, System.Text.Encoding.UTF8);

        text = Regex.Replace(
            text,
            @"(?s)<name>.*?</name>",
            $"<name>{master}</name>",
            RegexOptions.None,
            TimeSpan.FromSeconds(5));

        // Regex.Replace с count не поддерживается напрямую, делаем через Match
        text = ReplaceFirst(text, @"(?s)<name>.*?</name>", $"<name>{master}</name>");
        text = ReplaceFirst(text, @"(?s)<href>.*?</href>", $"<href>files/{master}.jpg</href>");

        File.WriteAllText(kmlPath, text, new System.Text.UTF8Encoding(false));
    }

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