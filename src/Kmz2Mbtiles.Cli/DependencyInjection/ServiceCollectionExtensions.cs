using Kmz2Mbtiles.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kmz2Mbtiles.Cli.DependencyInjection;

/// <summary>
/// Registers CLI services (progress reporter and application entry point) into a dependency
/// injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the CLI services required to run the application.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// The following services are registered:
    /// <list type="bullet">
    ///   <item><description><see cref="IProgressReporter"/> as a singleton backed by <see cref="SpectreProgressReporter"/>.</description></item>
    ///   <item><description><see cref="CliApp"/> as a singleton entry point.</description></item>
    /// </list>
    /// This method must be called in addition to <c>AddKmz2MbtilesCore</c>; the Core
    /// registrations depend on an <see cref="IProgressReporter"/> implementation being
    /// available at resolution time.
    /// </remarks>
    public static IServiceCollection AddKmz2MbtilesCli(this IServiceCollection services)
    {
        services.AddSingleton<IProgressReporter, SpectreProgressReporter>();
        services.AddSingleton<CliApp>();
        return services;
    }
}