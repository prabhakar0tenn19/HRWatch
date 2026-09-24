using Coravel;
using HRWatch.Application.Common.Auth;
using HRWatch.Application.Common.Interfaces;
using HRWatch.Infrastructure.Auth;
using HRWatch.Infrastructure.ExternalApis.Cg1;
using HRWatch.Infrastructure.ExternalApis.Cosec;
using HRWatch.Infrastructure.Persistence;
using HRWatch.Infrastructure.Scheduler;
using HRWatch.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRWatch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Database Context (Supports SQL Server locally and PostgreSQL / Supabase in Cloud)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=HRWatch2Db;Trusted_Connection=True;MultipleActiveResultSets=true";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (connectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("supabase", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Port=5432", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Port=6543", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

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
            var baseUrl = configuration["CG1:BaseUrl"] ?? "https://cg-one-ntier-dev.azurewebsites.net";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var baseUrl = configuration["CG1:BaseUrl"] ?? string.Empty;
            // Only bypass SSL certificate validation for local development mock servers
            if (baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        });

        // 3. Auth Token Service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // 4. SMTP Email Service
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // 5. Coravel Scheduler & Invocables
        services.AddScheduler();
        services.AddTransient<DailyAttendanceEvaluationJob>();
        services.AddTransient<DailyEmployeeSyncJob>();
        services.AddTransient<WeeklyViolatorsEmailJob>();

        return services;
    }
}
