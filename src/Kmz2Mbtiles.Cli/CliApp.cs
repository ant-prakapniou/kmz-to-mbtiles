using Kmz2Mbtiles.Core.Pipeline;

namespace Kmz2Mbtiles.Cli;

public sealed class CliApp
{
    private readonly KmzProcessor _processor;

    public CliApp(KmzProcessor processor)
    {
        _processor = processor;
    }

    public int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: drag and drop a .kmz file onto Kmz2Mbtiles.bat");
            return 1;
        }

        var anyFailed = false;

        foreach (var arg in args)
        {
            var path = arg.Trim('"');
            var fullPath = Path.GetFullPath(path);
            var baseDir = Path.GetDirectoryName(fullPath)!;
            var result = _processor.Process(fullPath, baseDir);
            if (!result.Success)
                anyFailed = true;
        }

        return anyFailed ? 1 : 0;
    }
}