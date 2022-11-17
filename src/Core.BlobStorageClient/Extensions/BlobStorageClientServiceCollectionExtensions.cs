using System;
using System.Diagnostics.CodeAnalysis;
using Core.BlobStorageClient.Interfaces;
using Core.BlobStorageClient.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Core.BlobStorageClient.Extensions;

public static class BlobStorageClientServiceCollectionExtensions
{
    public static IServiceCollection AddBlobStorageClient([NotNull] this IServiceCollection services, Action<BlobStorageClientOptions> setupAction)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (setupAction == null)
        {
            throw new ArgumentNullException(nameof(setupAction));
        }

        services.AddOptions();
        services.Configure(setupAction);
        services.Add(ServiceDescriptor.Singleton<IBlobStorageClient, BlobStorageClient>());

        return services;
    }
}