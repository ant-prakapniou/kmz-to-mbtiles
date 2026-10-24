using Kmz2Mbtiles.Core.Abstractions;
using Kmz2Mbtiles.Core.Geo;
using Kmz2Mbtiles.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Kmz2Mbtiles.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKmz2MbtilesCore(this IServiceCollection services)
    {
        services.AddSingleton<IGdalRunner, GdalRunner>();
        services.AddTransient<KmzProcessor>();
        return services;
    }
}