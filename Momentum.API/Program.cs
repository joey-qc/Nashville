using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Momentum.API.Converters;
using Momentum.Application.Interfaces;
using Momentum.Application.Services;
using Momentum.Infrastructure.Data;
using Momentum.Infrastructure.Identity;
using Momentum.Infrastructure.Repositories;
using Momentum.Infrastructure.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/momentum-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console()
        .WriteTo.File("logs/momentum-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter()));

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("MomentumDb"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null)));

    builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    var jwt = builder.Configuration.GetSection("Jwt");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwt["Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
            };
        });

    builder.Services.AddAuthorization();

    // Repositories
    builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
    builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
    builder.Services.AddScoped<ICheckInRepository, CheckInRepository>();

    // Application services
    builder.Services.AddScoped<IActivityService, ActivityService>();
    builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
    builder.Services.AddScoped<IScoreService, ScoreService>();
    builder.Services.AddScoped<ICheckInService, CheckInService>();

    // Infrastructure services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IActivitySeedService, ActivitySeedService>();
    builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(
                      "https://localhost:7202",
                      "http://localhost:5012",
                      "https://nice-ground-0e421bf1e.7.azurestaticapps.net")
                  .AllowAnyMethod()
                  .AllowAnyHeader()));

    var app = builder.Build();

    // Azure SQL Serverless auto-pauses when idle and returns error 40613 on the
    // first connection attempt. Retry the migration to allow the database time to
    // resume. If all retries fail, log and continue so the process stays alive —
    // individual requests will surface the error rather than killing the whole app.
    const int migrationMaxRetries = 5;
    const int migrationRetryDelaySeconds = 10;
    for (var attempt = 1; attempt <= migrationMaxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            break; // success — exit the retry loop
        }
        catch (SqlException ex) when (ex.Number == 40613)
        {
            if (attempt == migrationMaxRetries)
            {
                Log.Error(ex,
                    "Database migration failed after {MaxRetries} attempts — " +
                    "database unavailable (40613). App will start; requests may fail until the database resumes",
                    migrationMaxRetries);
                break;
            }
            Log.Warning(
                "Database unavailable (40613) on migration attempt {Attempt}/{MaxRetries}. " +
                "Retrying in {Delay}s...",
                attempt, migrationMaxRetries, migrationRetryDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(migrationRetryDelaySeconds));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database migration failed on attempt {Attempt}/{MaxRetries}",
                attempt, migrationMaxRetries);
            throw;
        }
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
