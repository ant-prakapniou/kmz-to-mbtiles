namespace Kmz2Mbtiles.Core.Abstractions;

/// <summary>
/// Abstraction over user-facing progress output produced by the conversion pipeline.
/// </summary>
/// <remarks>
/// The pipeline lives in the Core library and must not depend on any particular
/// console UI. Implementations of this interface decide how each kind of message
/// is rendered (for example, via Spectre.Console in the CLI project).
/// </remarks>
public interface IProgressReporter
{
    /// <summary>
    /// Writes a top-level header that visually separates one processing unit from another.
    /// </summary>
    /// <param name="title">Header text, typically the name of the file being processed.</param>
    void Header(string title);

    /// <summary>
    /// Writes a numbered pipeline step, for example <c>[1/5] Extracting KMZ...</c>.
    /// </summary>
    /// <param name="current">One-based index of the current step.</param>
    /// <param name="total">Total number of steps in the pipeline.</param>
    /// <param name="message">Description of the step.</param>
    void Step(int current, int total, string message);

    /// <summary>
    /// Writes a plain informational message, indented under the current step.
    /// </summary>
    /// <param name="message">Message text.</param>
    void Info(string message);

    /// <summary>
    /// Writes a detail line, indented further than <see cref="Info"/> for hierarchical output.
    /// </summary>
    /// <param name="message">Detail text, for example a metric or a GDAL command label.</param>
    void Detail(string message);

    /// <summary>
    /// Writes a labeled file system path, aligned with other path lines.
    /// </summary>
    /// <param name="label">Short label describing the path, for example <c>mbtiles</c>.</param>
    /// <param name="path">Full or relative path to report.</param>
    void Path(string label, string path);

    /// <summary>
    /// Writes a success message.
    /// </summary>
    /// <param name="message">Success text.</param>
    void Success(string message);

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    /// <param name="message">Warning text.</param>
    void Warn(string message);

    /// <summary>
    /// Writes an error message.
    /// </summary>
    /// <param name="message">Error text.</param>
    void Error(string message);
}