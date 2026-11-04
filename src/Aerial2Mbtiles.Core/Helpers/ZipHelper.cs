using System.Text;
using SharpCompress.Common;
using SharpCompress.Readers;
using ZipArchive = SharpCompress.Archives.Zip.ZipArchive;

namespace Aerial2Mbtiles.Core.Helpers;

/// <summary>
/// Extracts KMZ archives, which are ZIP containers holding a KML document and
/// its referenced assets.
/// </summary>
public static class ZipHelper
{
    /// <summary>
    /// Extracts all file entries from a KMZ archive into the specified directory.
    /// </summary>
    /// <param name="kmzPath">Path to the source KMZ archive.</param>
    /// <param name="dstDir">Destination directory. Created if it does not exist.</param>
    /// <remarks>
    /// Archive entry names are read using code page 437 as the default encoding and
    /// UTF-8 as a forced override. This combination preserves the original Cyrillic
    /// file names that Google Earth writes into KMZ archives, which are not always
    /// valid UTF-8.
    /// </remarks>
    public static void Extract(string kmzPath, string dstDir)
    {
        Directory.CreateDirectory(dstDir);

        var options = new ReaderOptions
        {
            ArchiveEncoding = new ArchiveEncoding
            {
                Default = Encoding.GetEncoding(437),
                Forced  = Encoding.UTF8,
            },
        };

        using var archive = ZipArchive.OpenArchive(kmzPath, options);
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory) continue;
            if (entry.Key is null) continue;

            var relative = entry.Key.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(dstDir, relative);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var input = entry.OpenEntryStream();
            using var output = File.Create(fullPath);
            input.CopyTo(output);
        }
    }
}