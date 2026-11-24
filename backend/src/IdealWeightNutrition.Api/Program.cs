using System.Text;
using FastEndpoints;
using IdealWeightNutrition.Api;
using IdealWeightNutrition.Api.Authorization;
using IdealWeightNutrition.Api.Hubs;
using IdealWeightNutrition.Application.Abstractions;
using FastEndpoints.Swagger;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Local, git-ignored overrides for secrets (payment gateways, SMTP, OAuth, etc.).
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "log-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true));

// 50 MB request bodies (video banner uploads).
const long MaxUploadBytes = 50L * 1024 * 1024;
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MaxUploadBytes);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = MaxUploadBytes);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
    .SetApplicationName("IdealWeightNutrition")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

builder.Services.AddProblemDetails();
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("PublicRead", policy => policy.Expire(TimeSpan.FromMinutes(5)));
});

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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // IIS/ANCM is not on loopback-with-known-proxy in every host (shared hosting); trust its headers.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services, app.Logger, app.Configuration);

app.UseForwardedHeaders();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var isBadRequest = exception is InvalidOperationException;
    var status = isBadRequest ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
    context.Response.StatusCode = status;
    var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = status,
        Title = isBadRequest ? exception!.Message : "An unexpected error occurred. Please try again later.",
        Detail = isBadRequest ? exception!.Message : null
    };
    await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
}));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();
app.UseCors("Spa");
app.UseRateLimiter();
app.UseLegacyMediaStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapGet("/sitemap.xml", async (ISeoService seo, CancellationToken ct) =>
{
    var xml = await seo.GenerateSitemapXmlAsync(ct);
    return Results.Content(xml, "application/xml", Encoding.UTF8);
}).AllowAnonymous().CacheOutput("PublicRead");

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
