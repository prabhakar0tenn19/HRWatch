using HRWatch.Application.Common.Abstractions;
using HRWatch.Infrastructure.Auth;
using HRWatch.Infrastructure.BackgroundJobs;
using HRWatch.Infrastructure.ExternalApis;
using HRWatch.Infrastructure.Persistence;
using HRWatch.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRWatch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));

        RegisterRepositories(services);

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        RegisterHttpClients(services, configuration);

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                configuration.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

        services.AddHangfireServer();

        services.AddScoped<AttendanceSyncJob>();
        services.AddScoped<GenerateWeeklyReportJob>();
        services.AddScoped<EmployeeSyncJob>();
        services.AddScoped<NotificationJob>();

        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IWeeklyReportRepository, WeeklyReportRepository>();
        services.AddScoped<IViolationRepository, ViolationRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
    }

    private static void RegisterHttpClients(IServiceCollection services, IConfiguration configuration)
    {
        var employeeBaseUrl = configuration["ExternalApis:EmployeeApi:BaseUrl"] ?? "https://localhost";

        services.AddHttpClient<IEmployeeWeeklyOverviewApiClient, EmployeeWeeklyOverviewApiClient>(client =>
        {
            client.BaseAddress = new Uri(employeeBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient<IEmployeeApiClient, EmployeeClient>(client =>
        {
            client.BaseAddress = new Uri(employeeBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient<IAttendanceApiClient, AttendanceClient>(client =>
        {
            client.BaseAddress = new Uri(employeeBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
    }
}
