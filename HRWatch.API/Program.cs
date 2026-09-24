using System.Text;
using Coravel;
using HRWatch.Application;
using HRWatch.Infrastructure;
using HRWatch.Infrastructure.Persistence;
using HRWatch.Infrastructure.Scheduler;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Add Layer Dependencies
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

// 3. Controllers & JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 4. JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "HRWatchSuperSecretKeyWithAtLeast32CharactersLong2026!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HRWatch";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HRWatchPortal";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. Swagger with JWT Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HRWatch 2.0 API",
        Version = "v1",
        Description = "Enterprise WFO Compliance & Biometric Attendance Evaluation Engine"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token format: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 6. CORS Policy
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("HRWatchCorsPolicy", policy =>
    {
        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// 7. Auto-migrate Database & Seed Default Policy / Admin
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var sqlScript = dbContext.Database.GenerateCreateScript();
                dbContext.Database.ExecuteSqlRaw(sqlScript);
            }
            catch (Exception ex)
            {
                Log.Information("PostgreSQL schema check: {Message}", ex.Message);
            }
            Log.Information("PostgreSQL (Supabase) database schema ensured.");
        }
        else
        {
            dbContext.Database.Migrate();
        }

        // 1. Seed Default WFO Policy Version 1 if none exists
        if (!dbContext.Policies.Any())
        {
            dbContext.Policies.Add(new HRWatch.Domain.Entities.Policy
            {
                Version = 1,
                PolicyName = "Default CG India WFO Policy",
                RulesJson = "{\"MinWfoDaysPerWeek\":{\"SDE\":5,\"Consultant\":5,\"Intern\":5,\"Associate\":3,\"Manager\":3,\"Principal\":3,\"Bench\":5},\"DefaultRequiredDays\":5}",
                EffectiveFrom = DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
                IsActive = true,
                CreatedBy = "SystemInitialSeed"
            });
            dbContext.SaveChanges();
            Log.Information("Successfully seeded default WFO Policy Version 1.");
        }

        // 2. Seed Default Admin User if none exists or ensure admin credentials
        var adminUser = dbContext.Users.FirstOrDefault(u => u.Username == "admin");
        if (adminUser == null)
        {
            dbContext.Users.Add(new HRWatch.Domain.Entities.User
            {
                Username = "admin",
                Email = "admin@cginfinity.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                Role = HRWatch.Domain.Enums.UserRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            dbContext.SaveChanges();
            Log.Information("Successfully seeded default Admin User: 'admin' / 'Admin@1234'.");
        }
        else
        {
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234");
            adminUser.IsActive = true;
            dbContext.SaveChanges();
            Log.Information("Synchronized Admin User password: 'admin' / 'Admin@1234'.");
        }
    }
    catch (Exception ex)
    {
        Log.Warning("Database migration/seed note: {Message}", ex.Message);
    }
}

// 8. Coravel Fluent Scheduler Setup
app.Services.UseScheduler(scheduler =>
{
    var istZone = HRWatch.Domain.Common.IndiaDateTime.TimeZone;

    // Daily 11:30 PM IST: Daily Evaluation Job
    scheduler.Schedule<DailyAttendanceEvaluationJob>()
        .DailyAt(23, 30)
        .Zoned(istZone);

    // Daily 12:00 AM IST: Employee Master Sync Job
    scheduler.Schedule<DailyEmployeeSyncJob>()
        .DailyAt(0, 0)
        .Zoned(istZone);

    // Sunday 10:00 PM IST (22:00): Weekly Violators Email Summary Job
    scheduler.Schedule<WeeklyViolatorsEmailJob>()
        .Cron("0 22 * * 0")
        .Zoned(istZone);
});

// 9. HTTP Pipeline
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HRWatch 2.0 API v1"));
}

app.UseCors("HRWatchCorsPolicy");
app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
