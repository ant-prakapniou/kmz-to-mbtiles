namespace Kmz2Mbtiles.Core.Abstractions;

public interface IProgressReporter
{
    void Header(string title);
    void Step(int current, int total, string message);
    void Info(string message);
    void Detail(string message);
    void Path(string label, string path);
    void Success(string message);
    void Warn(string message);
    void Error(string message);
}