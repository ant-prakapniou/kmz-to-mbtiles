using Kmz2Mbtiles.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kmz2Mbtiles.Cli.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKmz2MbtilesCli(this IServiceCollection services)
    {
        services.AddSingleton<IProgressReporter, SpectreProgressReporter>();
        services.AddSingleton<CliApp>();
        return services;
    }
}