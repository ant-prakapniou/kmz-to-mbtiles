using Kmz2Mbtiles.Core.Abstractions;
using Kmz2Mbtiles.Core.Geo;
using Kmz2Mbtiles.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Kmz2Mbtiles.Core.DependencyInjection;

/// <summary>
/// Registers Core services (pipeline and GDAL abstraction) into a dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Core services required to run the KMZ-to-MBTiles conversion pipeline.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// The following services are registered:
    /// <list type="bullet">
    ///   <item><description><see cref="IGdalRunner"/> as a singleton backed by <see cref="GdalRunner"/>.</description></item>
    ///   <item><description><see cref="KmzProcessor"/> as a transient pipeline orchestrator.</description></item>
    /// </list>
    /// Implementations of <see cref="IProgressReporter"/> are intentionally not registered here;
    /// the host application (for example, the CLI project) is responsible for providing one.
    /// </remarks>
    public static IServiceCollection AddKmz2MbtilesCore(this IServiceCollection services)
    {
        services.AddSingleton<IGdalRunner, GdalRunner>();
        services.AddTransient<KmzProcessor>();
        return services;
    }
}