using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;
using System.IO.Compression;
using System.Text;
using ZipArchive = SharpCompress.Archives.Zip.ZipArchive;

namespace Kmz2Mbtiles.Core.Kmz;

public static class ZipHelper
{
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

            var relative = entry.Key.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(dstDir, relative);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var input = entry.OpenEntryStream();
            using var output = File.Create(fullPath);
            input.CopyTo(output);
        }
    }

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