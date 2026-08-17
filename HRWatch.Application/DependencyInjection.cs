using HRWatch.Domain.Services;
using LiteBus.Commands;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Queries;
using LiteBus.Runtime.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HRWatch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IWfoEvaluationService, WfoEvaluationService>();

        services.AddLiteBus(builder =>
        {
            if (builder is IModuleRegistry registry)
            {
                registry.AddCommandModule(cfg => cfg.RegisterFromAssembly(typeof(DependencyInjection).Assembly));
                registry.AddQueryModule(cfg => cfg.RegisterFromAssembly(typeof(DependencyInjection).Assembly));
            }
        });

        return services;
    }
}
