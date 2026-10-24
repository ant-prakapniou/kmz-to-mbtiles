using System.Text;
using SharpCompress.Common;
using SharpCompress.Writers;
using ZipArchive = SharpCompress.Archives.Zip.ZipArchive;

namespace Kmz2Mbtiles.Core.Kmz;

/// <summary>
/// Creates and extracts KMZ archives, which are ZIP containers holding a KML document
/// and its referenced assets.
/// </summary>
public static class ZipHelper
{
    /// <summary>
    /// Extracts all file entries from a KMZ archive into the specified directory.
    /// </summary>
    /// <param name="kmzPath">Path to the source KMZ archive.</param>
    /// <param name="dstDir">Destination directory. Created if it does not exist.</param>
    /// <remarks>
    /// Archive entry names are read using code page 437 as the default encoding and UTF-8
    /// as a forced override. This combination preserves the original Cyrillic file names
    /// that Google Earth writes into KMZ archives, which are not always valid UTF-8.
    /// </remarks>
    public static void Extract(string kmzPath, string dstDir)
    {
        Directory.CreateDirectory(dstDir);

        var options = new SharpCompress.Readers.ReaderOptions
        {
            ArchiveEncoding = new ArchiveEncoding
            {
                Default = Encoding.GetEncoding(437),
                Forced = Encoding.UTF8,
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

    /// <summary>
    /// Packs all files under the specified directory into a new KMZ archive.
    /// </summary>
    /// <param name="srcDir">Source directory whose contents are packed recursively.</param>
    /// <param name="outKmz">Path to the output KMZ archive. Overwritten if it already exists.</param>
    /// <remarks>
    /// Entry names are written in UTF-8 and use forward slashes as separators, matching
    /// the ZIP specification and the convention used by Google Earth. The archive uses
    /// Deflate compression.
    /// </remarks>
    public static void Build(string srcDir, string outKmz)
    {
        if (File.Exists(outKmz)) File.Delete(outKmz);

        var options = new WriterOptions(CompressionType.Deflate)
        {
            ArchiveEncoding = new ArchiveEncoding
            {
                Default = Encoding.UTF8,
                Forced = Encoding.UTF8,
            },
        };

        using var zip = File.Create(outKmz);
        using var writer = WriterFactory.OpenWriter(zip, ArchiveType.Zip, options);

        foreach (var file in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(srcDir, file)
                               .Replace(Path.DirectorySeparatorChar, '/');
            writer.Write(relative, file);
        }
    }
}