using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Aerial2Mbtiles.Core.Helpers;

/// <summary>
/// Reads and patches KML documents that describe a ground overlay of a raster image.
/// Supports both <c>&lt;gx:LatLonQuad&gt;</c> and <c>&lt;LatLonBox&gt;</c> binding styles
/// on input, and always writes <c>&lt;gx:LatLonQuad&gt;</c> on output.
/// </summary>
public static class KmlHelper
{
    private static readonly XNamespace GxNs = "http://www.google.com/kml/ext/2.2";
    private static readonly XNamespace KmlNs = "http://www.opengis.net/kml/2.2";

    // WGS84
    private const double Wgs84A = 6378137.0;
    private const double Wgs84F = 1.0 / 298.257223563;
    private const double Wgs84B = Wgs84A * (1.0 - Wgs84F);

    /// <summary>
    /// Reads the raw <c>&lt;name&gt;</c> and <c>&lt;href&gt;</c> values from the first
    /// <c>&lt;GroundOverlay&gt;</c> in a KML document, reduced to their base form
    /// (no directory, no extension) for display and comparison.
    /// </summary>
    /// <param name="kmlPath">Path to the KML file to read.</param>
    /// <returns>
    /// A tuple with the base name (from <c>&lt;name&gt;</c>) and the base href
    /// (from <c>&lt;href&gt;</c>, with directory and extension removed).
    /// Either value is <see langword="null"/> if the corresponding element is missing.
    /// </returns>
    public static (string? Name, string? HrefCore) ReadNames(string kmlPath)
    {
        var doc = XDocument.Load(kmlPath);
        var overlay = doc.Descendants(KmlNs + "GroundOverlay").FirstOrDefault()
            ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "GroundOverlay");

        if (overlay is null)
            return (null, null);

        var name = overlay.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "name")?.Value.Trim();

        var hrefRaw = overlay.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "Icon")?
            .Elements()
            .FirstOrDefault(e => e.Name.LocalName == "href")?.Value.Trim();

        string? hrefCore = null;
        if (!string.IsNullOrEmpty(hrefRaw))
        {
            var lastSlash = hrefRaw.LastIndexOfAny(['/', '\\']);
            var fileName = lastSlash >= 0 ? hrefRaw[(lastSlash + 1)..] : hrefRaw;
            hrefCore = Path.GetFileNameWithoutExtension(fileName);
        }

        return (name, hrefCore);
    }

    // ------------------------------------------------------------------
    // Parsing
    // ------------------------------------------------------------------

    public static (double Lon, double Lat)[] ParseBinding(string kmlPath)
    {
        var doc = XDocument.Load(kmlPath);

        if (doc.Descendants(GxNs + "LatLonQuad").Any())
            return ParseLatLonQuad(doc);

        if (FindLatLonBox(doc) is not null)
            return ParseLatLonBox(doc);

        throw new InvalidOperationException(
            "no <gx:LatLonQuad> or <LatLonBox> element found in KML");
    }

    public static (double Lon, double Lat)[] ParseLatLonQuad(string kmlPath)
        => ParseLatLonQuad(XDocument.Load(kmlPath));

    private static (double Lon, double Lat)[] ParseLatLonQuad(XDocument doc)
    {
        var coords = doc.Descendants(GxNs + "LatLonQuad")
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "coordinates")
            ?? throw new InvalidOperationException("no <gx:LatLonQuad> element found in KML");

        var tokens = coords.Value
            .Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        var result = new List<(double, double)>();
        foreach (var token in tokens)
        {
            var parts = token.Split(',');
            if (parts.Length < 2) continue;
            result.Add((
                double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture)));
        }

        if (result.Count != 4)
            throw new InvalidOperationException(
                $"expected 4 corners in LatLonQuad, found {result.Count}");

        return [.. result];
    }

    public static (double Lon, double Lat)[] ParseLatLonBox(string kmlPath)
        => ParseLatLonBox(XDocument.Load(kmlPath));

    private static (double Lon, double Lat)[] ParseLatLonBox(XDocument doc)
    {
        var box = FindLatLonBox(doc)
            ?? throw new InvalidOperationException("no <LatLonBox> element found in KML");

        var north = ReadDouble(box, "north");
        var south = ReadDouble(box, "south");
        var east = ReadDouble(box, "east");
        var west = ReadDouble(box, "west");

        var rotationElement = box.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "rotation");
        var rotation = rotationElement is null
            ? 0.0
            : double.Parse(rotationElement.Value.Trim(), CultureInfo.InvariantCulture);

        // Center of the box
        var centerLat = (north + south) / 2.0;
        var centerLon = (east + west) / 2.0;

        // Half-height: distance south -> north along the meridian at centerLon
        var halfHeightM = VincentyInverse(centerLon, south, centerLon, north) / 2.0;

        // Half-width: distance west -> east along the parallel at centerLat
        var halfWidthM = VincentyInverse(west, centerLat, east, centerLat) / 2.0;

        // Diagonal distance from center to a corner
        var diagDist = Math.Sqrt(halfWidthM * halfWidthM + halfHeightM * halfHeightM);

        // Base azimuths (0 = north, clockwise) from center to each corner
        double BaseAzimuth(double eastComp, double northComp) =>
            Math.Atan2(eastComp, northComp) * 180.0 / Math.PI;

        var azSW = BaseAzimuth(-halfWidthM, -halfHeightM);
        var azSE = BaseAzimuth(halfWidthM, -halfHeightM);
        var azNE = BaseAzimuth(halfWidthM, halfHeightM);
        var azNW = BaseAzimuth(-halfWidthM, halfHeightM);

        // KML rotation: as in the verified script, subtract.
        var azimuths = new[]
        {
            (azSW - rotation) % 360.0,
            (azSE - rotation) % 360.0,
            (azNE - rotation) % 360.0,
            (azNW - rotation) % 360.0,
        };

        // Forward-project each corner from the center
        var result = new (double Lon, double Lat)[4];
        for (var i = 0; i < 4; i++)
        {
            var (lon, lat) = VincentyForward(centerLat, centerLon, azimuths[i], diagDist);
            result[i] = (lon, lat);
        }

        return result;
    }

    private static XElement? FindLatLonBox(XDocument doc)
        => doc.Descendants(KmlNs + "LatLonBox").FirstOrDefault()
        ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "LatLonBox");

    private static double ReadDouble(XElement parent, string localName)
    {
        var element = parent.Elements()
            .FirstOrDefault(e => e.Name.LocalName == localName)
            ?? throw new InvalidOperationException(
                $"<LatLonBox> is missing required <{localName}> element");

        return double.Parse(element.Value.Trim(), CultureInfo.InvariantCulture);
    }

    // ------------------------------------------------------------------
    // Geodesy: Vincenty inverse (distance) and forward (destination)
    // ------------------------------------------------------------------

    /// <summary>
    /// Vincenty inverse: great-circle distance on the WGS84 ellipsoid, in meters.
    /// </summary>
    private static double VincentyInverse(double lon1, double lat1, double lon2, double lat2)
    {
        var L = (lon2 - lon1) * Math.PI / 180.0;
        var U1 = Math.Atan((1 - Wgs84F) * Math.Tan(lat1 * Math.PI / 180.0));
        var U2 = Math.Atan((1 - Wgs84F) * Math.Tan(lat2 * Math.PI / 180.0));

        var sinU1 = Math.Sin(U1); var cosU1 = Math.Cos(U1);
        var sinU2 = Math.Sin(U2); var cosU2 = Math.Cos(U2);

        var lambda = L;
        double lambdaP;
        double sinSigma, cosSigma, sigma, sinAlpha, cosSqAlpha, cos2SigmaM;
        var iterLimit = 200;

        do
        {
            var sinLambda = Math.Sin(lambda);
            var cosLambda = Math.Cos(lambda);
            sinSigma = Math.Sqrt(
                Math.Pow(cosU2 * sinLambda, 2) +
                Math.Pow(cosU1 * sinU2 - sinU1 * cosU2 * cosLambda, 2));
            if (sinSigma == 0) return 0; // coincident points

            cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            sigma = Math.Atan2(sinSigma, cosSigma);
            sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            cosSqAlpha = 1 - sinAlpha * sinAlpha;
            cos2SigmaM = cosSqAlpha == 0
                ? 0
                : cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;

            var C = Wgs84F / 16.0 * cosSqAlpha * (4 + Wgs84F * (4 - 3 * cosSqAlpha));
            lambdaP = lambda;
            lambda = L + (1 - C) * Wgs84F * sinAlpha *
                (sigma + C * sinSigma *
                    (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
        } while (Math.Abs(lambda - lambdaP) > 1e-12 && --iterLimit > 0);

        if (iterLimit == 0)
            throw new InvalidOperationException("Vincenty inverse did not converge");

        var uSq = cosSqAlpha * (Wgs84A * Wgs84A - Wgs84B * Wgs84B) / (Wgs84B * Wgs84B);
        var A = 1 + uSq / 16384.0 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
        var B = uSq / 1024.0 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
        var deltaSigma = B * sinSigma *
            (cos2SigmaM + B / 4.0 *
                (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)
                 - B / 6.0 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma)
                                * (-3 + 4 * cos2SigmaM * cos2SigmaM)));

        return Wgs84B * A * (sigma - deltaSigma);
    }

    /// <summary>
    /// Vincenty forward: destination point on the WGS84 ellipsoid given
    /// start point, azimuth (degrees, clockwise from north) and distance (meters).
    /// Returns (lon, lat) in degrees.
    /// </summary>
    private static (double Lon, double Lat) VincentyForward(
        double lat1, double lon1, double azimuthDeg, double distanceM)
    {
        var alpha1 = azimuthDeg * Math.PI / 180.0;

        var sinAlpha1 = Math.Sin(alpha1);
        var cosAlpha1 = Math.Cos(alpha1);

        var tanU1 = (1 - Wgs84F) * Math.Tan(lat1 * Math.PI / 180.0);
        var cosU1 = 1.0 / Math.Sqrt(1 + tanU1 * tanU1);
        var sinU1 = tanU1 * cosU1;

        var sigma1 = Math.Atan2(tanU1, cosAlpha1);
        var sinAlpha = cosU1 * sinAlpha1;
        var cosSqAlpha = 1 - sinAlpha * sinAlpha;
        var uSq = cosSqAlpha * (Wgs84A * Wgs84A - Wgs84B * Wgs84B) / (Wgs84B * Wgs84B);

        var A = 1 + uSq / 16384.0 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
        var B = uSq / 1024.0 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));

        var sigma = distanceM / (Wgs84B * A);
        double sigmaP;
        double cos2SigmaM, sinSigma, cosSigma;
        var iterLimit = 200;

        do
        {
            cos2SigmaM = Math.Cos(2 * sigma1 + sigma);
            sinSigma = Math.Sin(sigma);
            cosSigma = Math.Cos(sigma);
            var deltaSigma = B * sinSigma *
                (cos2SigmaM + B / 4.0 *
                    (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)
                     - B / 6.0 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma)
                                    * (-3 + 4 * cos2SigmaM * cos2SigmaM)));
            sigmaP = sigma;
            sigma = distanceM / (Wgs84B * A) + deltaSigma;
        } while (Math.Abs(sigma - sigmaP) > 1e-12 && --iterLimit > 0);

        if (iterLimit == 0)
            throw new InvalidOperationException("Vincenty forward did not converge");

        var tmp = sinU1 * sinSigma - cosU1 * cosSigma * cosAlpha1;
        var lat2 = Math.Atan2(
            sinU1 * cosSigma + cosU1 * sinSigma * cosAlpha1,
            (1 - Wgs84F) * Math.Sqrt(sinAlpha * sinAlpha + tmp * tmp));

        var lambda = Math.Atan2(
            sinSigma * sinAlpha1,
            cosU1 * cosSigma - sinU1 * sinSigma * cosAlpha1);

        var C = Wgs84F / 16.0 * cosSqAlpha * (4 + Wgs84F * (4 - 3 * cosSqAlpha));
        var L = lambda - (1 - C) * Wgs84F * sinAlpha *
            (sigma + C * sinSigma *
                (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));

        var lon2 = lon1 * Math.PI / 180.0 + L;

        // Normalize lon to [-180, 180]
        lon2 = (lon2 + 3 * Math.PI) % (2 * Math.PI) - Math.PI;

        return (lon2 * 180.0 / Math.PI, lat2 * 180.0 / Math.PI);
    }

    // ------------------------------------------------------------------
    // Patching
    // ------------------------------------------------------------------

    public static void PatchKml(
        string kmlPath,
        string master,
        string rasterExtension,
        (double Lon, double Lat)[] corners)
    {
        if (corners.Length != 4)
            throw new ArgumentException("expected exactly four corners", nameof(corners));

        var doc = XDocument.Load(kmlPath);

        var overlay = doc.Descendants(KmlNs + "GroundOverlay").FirstOrDefault()
            ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "GroundOverlay")
            ?? throw new InvalidOperationException("no <GroundOverlay> element found in KML");

        var nameElement = overlay.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "name")
            ?? throw new InvalidOperationException(
                "<GroundOverlay> is missing <name>; fix the input KML by hand");
        nameElement.Value = master;

        var iconElement = overlay.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "Icon")
            ?? throw new InvalidOperationException(
                "<GroundOverlay> is missing <Icon>; fix the input KML by hand");

        var hrefElement = iconElement.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "href")
            ?? throw new InvalidOperationException(
                "<Icon> is missing <href>; fix the input KML by hand");
        hrefElement.Value = master + rasterExtension;

        var existingBinding =
            overlay.Elements().FirstOrDefault(e => e.Name.LocalName == "LatLonBox")
            ?? overlay.Elements().FirstOrDefault(e =>
                   e.Name.Namespace == GxNs && e.Name.LocalName == "LatLonQuad");

        var newQuad = new XElement(GxNs + "LatLonQuad",
            new XElement("coordinates",
                string.Join(" ", corners.Select(c =>
                    $"{c.Lon.ToString("R", CultureInfo.InvariantCulture)}," +
                    $"{c.Lat.ToString("R", CultureInfo.InvariantCulture)}"))));

        if (existingBinding is not null)
        {
            existingBinding.ReplaceWith(newQuad);
        }
        else
        {
            var anchor = overlay.Elements().FirstOrDefault(e =>
                e.Name.LocalName is "altitude" or "altitudeMode");

            if (anchor is not null)
                anchor.AddBeforeSelf(newQuad);
            else
                overlay.Add(newQuad);
        }

        var settings = new System.Xml.XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
        };

        using var stream = File.Create(kmlPath);
        using var writer = System.Xml.XmlWriter.Create(stream, settings);
        doc.Save(writer);
    }
}