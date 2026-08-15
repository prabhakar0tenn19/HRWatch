using HRWatch.API.Extensions;
using HRWatch.API.Middleware;
using HRWatch.Application;
using HRWatch.Infrastructure;
using HRWatch.Infrastructure.BackgroundJobs;
using Hangfire;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "HRWatch")
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/hrwatch-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting HRWatch API...");

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerExtensions();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddHealthCheckExtensions(builder.Configuration);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("HRWatchCors", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRWatch API v1");
        c.RoutePrefix = string.Empty;
    });

    app.UseCors("HRWatchCors");

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health");

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = []
    });

    RecurringJob.AddOrUpdate<AttendanceSyncJob>(
        "attendance-daily-sync",
        job => job.ExecuteAsync(),
        Cron.Daily(hour: 1));

    RecurringJob.AddOrUpdate<GenerateWeeklyReportJob>(
        "weekly-report-generation",
        job => job.ExecuteAsync(),
        Cron.Weekly(DayOfWeek.Monday, hour: 6));

    RecurringJob.AddOrUpdate<EmployeeSyncJob>(
        "employee-daily-sync",
        job => job.ExecuteAsync(),
        Cron.Daily(hour: 0));

    RecurringJob.AddOrUpdate<NotificationJob>(
        "notification-daily",
        job => job.ExecuteAsync(),
        Cron.Daily(hour: 9));

    Log.Information("HRWatch API started successfully.");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "HRWatch API failed to start!");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
