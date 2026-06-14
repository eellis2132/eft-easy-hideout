using EasyHideout.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EasyHideout.Services;

public static class ServiceLocator
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider => _provider
        ?? throw new InvalidOperationException("ServiceLocator not initialized.");

    public static void Initialize(IServiceProvider provider) => _provider = provider;

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}
