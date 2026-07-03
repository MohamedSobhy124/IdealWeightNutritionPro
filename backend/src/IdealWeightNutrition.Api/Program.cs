using System.Text;
using FastEndpoints;
using IdealWeightNutrition.Api;
using IdealWeightNutrition.Api.Authorization;
using IdealWeightNutrition.Api.Hubs;
using IdealWeightNutrition.Application.Abstractions;
using FastEndpoints.Swagger;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Local, git-ignored overrides for secrets (payment gateways, SMTP, OAuth, etc.).
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

builder.Services.AddModernizedPlatform(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? new JwtSettings();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

var authBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
        };
    });

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder
        .AddCookie(ExternalAuthSchemes.ExternalCookie, options =>
        {
            options.Cookie.Name = "iwn.external";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        })
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = ExternalAuthSchemes.ExternalCookie;
            options.SaveTokens = true;
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });
}

builder.Services.AddApiAuthorization();
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(options =>
{
    options.DocumentSettings = settings =>
    {
        settings.Title = "Ideal Weight Nutrition API";
        settings.Version = "v1";
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Spa", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DatabaseInitializer.InitializeAsync(app.Services, app.Logger);
}

app.UseSerilogRequestLogging();
app.UseCors("Spa");
app.UseRateLimiter();

var legacyWwwRoot = Path.GetFullPath(Path.Combine(
    app.Environment.ContentRootPath,
    "..", "..", "..", "..",
    "IdealWeightNutrition",
    "wwwroot"));
if (Directory.Exists(legacyWwwRoot))
{
    var videosPath = Path.Combine(legacyWwwRoot, "videos");
    if (Directory.Exists(videosPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(videosPath),
            RequestPath = "/videos"
        });
    }

    var imagesPath = Path.Combine(legacyWwwRoot, "images");
    if (Directory.Exists(imagesPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagesPath),
            RequestPath = "/images"
        });
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/sitemap.xml", async (ISeoService seo, CancellationToken ct) =>
{
    var xml = await seo.GenerateSitemapXmlAsync(ct);
    return Results.Content(xml, "application/xml", Encoding.UTF8);
}).AllowAnonymous();

app.MapGet("/robots.txt", (ISeoService seo) =>
    Results.Content(seo.GenerateRobotsTxt(), "text/plain", Encoding.UTF8)).AllowAnonymous();

app.MapExternalAuthEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
}

app.UseFastEndpoints(config => config.Endpoints.RoutePrefix = "api");
app.UseSwaggerGen();
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var swaggerBase = app.Urls.FirstOrDefault(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        ?? app.Urls.FirstOrDefault()
        ?? "https://localhost:7128";
    var baseUrl = swaggerBase.TrimEnd('/');
    app.Logger.LogInformation("Swagger UI: {Url}/swagger", baseUrl);
    app.Logger.LogInformation("Health check: {Url}/api/health", baseUrl);
    app.Logger.LogInformation("Sitemap: {Url}/sitemap.xml", baseUrl);

    var smtpHost = app.Configuration["Smtp:Host"];
    var smtpUser = app.Configuration["Smtp:Username"];
    var smtpPassword = app.Configuration["Smtp:Password"];
    if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPassword))
    {
        app.Logger.LogWarning(
            "SMTP is not fully configured (Smtp:Host, Smtp:Username, Smtp:Password). Emails will be logged only.");
    }
});

app.Run();
