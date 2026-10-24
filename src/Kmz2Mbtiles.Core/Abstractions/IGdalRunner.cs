namespace Kmz2Mbtiles.Core.Abstractions;

public interface IGdalRunner
{
    bool Run(string exe, string label, params string[] args);
    (int W, int H) GetJpgSize(string jpgPath);
}