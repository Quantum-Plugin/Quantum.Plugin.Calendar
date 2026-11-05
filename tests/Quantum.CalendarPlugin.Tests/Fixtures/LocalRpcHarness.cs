using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NOF.Application;
using NOF.Contract;
using NOF.Hosting;
using NOF.Infrastructure;
using System.Reflection;

namespace Quantum.CalendarPlugin.Tests.Fixtures;

internal sealed class LocalRpcHarness : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    public LocalRpcHarness(IServiceCollection services, Assembly assembly)
    {
        AddPipelineServices(services);
        foreach (var initializer in assembly.GetCustomAttributes<NOF.Abstraction.AssemblyInitializeAttribute>())
        {
            initializer.Initialize(services);
        }

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public static void AddPipelineServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddNOFApplication();
        services.TryAddScoped<RpcServerInvocationResolver>();
        services.TryAddScoped<RequestInboundPipelineExecutor>();
        services.TryAddScoped<IRequestOutboundPipelineExecutor, RequestOutboundPipelineExecutor>();
        services.AddRequestInboundMiddleware<TracingInboundMiddleware>();
    }

    public async Task UseClient<TClient>(Func<TClient, Task> action)
        where TClient : notnull
    {
        await using var scope = _provider.CreateAsyncScope();
        scope.ServiceProvider.ResolveDaemonServices();
        await action(scope.ServiceProvider.GetRequiredService<TClient>());
    }

    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}
