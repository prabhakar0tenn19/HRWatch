using Coravel;
using HRWatch.Application.Common.Auth;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Infrastructure.Auth;
using HRWatch.Infrastructure.ExternalApis.Cg1;
using HRWatch.Infrastructure.ExternalApis.Cosec;
using HRWatch.Infrastructure.Persistence;
using HRWatch.Infrastructure.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRWatch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Context
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=HRWatch2Db;Trusted_Connection=True;MultipleActiveResultSets=true";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // 2. HTTP Clients for External APIs
        services.AddHttpClient<ICosecBiometricApiClient, CosecBiometricApiClient>(client =>
        {
            var baseUrl = configuration["Cosec:BaseUrl"] ?? "http://172.24.120.88";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<ICg1ApiClient, Cg1ApiClient>(client =>
        {
            var baseUrl = configuration["CG1:BaseUrl"] ?? "https://localhost:5092";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        // 3. Auth Token Service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // 4. Coravel Scheduler & Invocables
        services.AddScheduler();
        services.AddTransient<DailyAttendanceEvaluationJob>();
        services.AddTransient<DailyEmployeeSyncJob>();

        return services;
    }
}
