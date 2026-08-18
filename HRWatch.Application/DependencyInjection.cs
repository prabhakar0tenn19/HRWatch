using HRWatch.Domain.Services;
using LiteBus.Commands;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Messaging;
using LiteBus.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace HRWatch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IWfoEvaluationService, WfoEvaluationService>();

        services.AddLiteBus(builder =>
        {
            builder.AddMessaging(_ => { });
            builder.AddCommands(cfg => cfg.RegisterFromAssembly(typeof(DependencyInjection).Assembly));
            builder.AddQueries(cfg => cfg.RegisterFromAssembly(typeof(DependencyInjection).Assembly));
        });

        return services;
    }
}
