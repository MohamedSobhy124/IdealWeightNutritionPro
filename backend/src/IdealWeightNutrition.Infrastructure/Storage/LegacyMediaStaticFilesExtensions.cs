using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace IdealWeightNutrition.Infrastructure.Storage;

public static class LegacyMediaStaticFilesExtensions
{
    public static IApplicationBuilder UseLegacyMediaStaticFiles(this IApplicationBuilder app)
    {
        var resolver = app.ApplicationServices.GetRequiredService<LegacyWwwRootPathResolver>();
        if (!resolver.Exists)
            return app;

        var wwwRoot = resolver.Path;

        MapFolder(app, Path.Combine(wwwRoot, "videos"), "/videos");
        MapFolder(app, Path.Combine(wwwRoot, "images"), "/images");
        MapFolder(app, Path.Combine(wwwRoot, "Images"), "/Images");

        return app;
    }

    private static void MapFolder(IApplicationBuilder app, string physicalPath, string requestPath)
    {
        if (!Directory.Exists(physicalPath))
            return;

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalPath),
            RequestPath = requestPath
        });
    }
}
