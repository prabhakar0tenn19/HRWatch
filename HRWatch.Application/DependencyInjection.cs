using System.Reflection;
using FluentValidation;
using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Common.Mediator;
using HRWatch.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HRWatch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ICommandMediator, CommandMediator>();
        services.AddSingleton<IQueryMediator, QueryMediator>();

        services.AddTransient<PolicyEngine>();
        services.AddTransient<ComplianceEvaluator>();
        services.AddTransient<RuleEvaluator>();
        services.AddTransient<IViolationCalculationService, ViolationCalculationService>();

        RegisterCommandHandlers(services);

        RegisterQueryHandlers(services);

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    private static void RegisterCommandHandlers(IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                .Select(i => new { Implementation = t, Interface = i }));

        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }
    }

    private static void RegisterQueryHandlers(IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                .Select(i => new { Implementation = t, Interface = i }));

        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }
    }
}
