using System.Text;
using System.Xml.Linq;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class SeoService : ISeoService
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<SeoService> _logger;

    public SeoService(
        AppDbContext db,
        IConfiguration configuration,
        IDateTimeProvider clock,
        ILogger<SeoService> logger)
    {
        _db = db;
        _configuration = configuration;
        _clock = clock;
        _logger = logger;
    }

    public async Task<string> GenerateSitemapXmlAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        var now = _clock.Now;
        var urls = new List<XElement>
        {
            CreateUrl(baseUrl, now, "daily", 1.0),
            CreateUrl($"{baseUrl}/shop", now, "daily", 0.9),
            CreateUrl($"{baseUrl}/offers", now.AddDays(-1), "weekly", 0.9),
            CreateUrl($"{baseUrl}/flash-sales", now, "daily", 0.9),
            CreateUrl($"{baseUrl}/combos", now.AddDays(-1), "weekly", 0.9),
            CreateUrl($"{baseUrl}/services", now.AddDays(-1), "weekly", 0.9),
            CreateUrl($"{baseUrl}/blog", now, "daily", 0.9),
            CreateUrl($"{baseUrl}/track", now.AddDays(-7), "monthly", 0.5),
            CreateUrl($"{baseUrl}/page/about", now.AddDays(-7), "monthly", 0.8),
            CreateUrl($"{baseUrl}/page/privacy", now.AddDays(-7), "monthly", 0.5),
            CreateUrl($"{baseUrl}/page/terms", now.AddDays(-7), "monthly", 0.5),
            CreateUrl($"{baseUrl}/page/shipping", now.AddDays(-7), "monthly", 0.7),
            CreateUrl($"{baseUrl}/page/return-policy", now.AddDays(-7), "monthly", 0.7),
            CreateUrl($"{baseUrl}/page/help", now.AddDays(-7), "monthly", 0.8),
        };

        try
        {
            var products = await _db.Products.AsNoTracking()
                .Where(p => !p.IsDeleted)
                .Select(p => new { p.SlugEn, p.Id })
                .ToListAsync(cancellationToken);

            urls.AddRange(products.Select(p =>
                CreateUrl($"{baseUrl}/product/{(string.IsNullOrEmpty(p.SlugEn) ? p.Id : p.SlugEn)}", now.AddDays(-3), "weekly", 0.8)));

            var categories = await _db.Categories.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            urls.AddRange(categories.Select(id =>
                CreateUrl($"{baseUrl}/shop?categoryId={id}", now.AddDays(-7), "weekly", 0.7)));

            var services = await _db.ServiceSubscriptions.AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            urls.AddRange(services.Select(id =>
                CreateUrl($"{baseUrl}/services/{id}", now.AddDays(-7), "monthly", 0.7)));

            var flashSales = await _db.FlashSales.AsNoTracking()
                .Where(f => !f.IsDeleted && f.IsActive)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);

            urls.AddRange(flashSales.Select(id =>
                CreateUrl($"{baseUrl}/flash-sales/{id}", now, "daily", 0.9)));

            var combos = await _db.ComboOffers.AsNoTracking()
                .Where(c => !c.IsDeleted && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            urls.AddRange(combos.Select(id =>
                CreateUrl($"{baseUrl}/combos/{id}", now.AddDays(-3), "weekly", 0.8)));

            var posts = await _db.BlogPosts.AsNoTracking()
                .Where(b => !b.IsDeleted)
                .Select(b => new { b.Slug, b.PublishedDate })
                .ToListAsync(cancellationToken);

            urls.AddRange(posts.Select(p =>
                CreateUrl($"{baseUrl}/blog/{p.Slug}", p.PublishedDate, "monthly", 0.7)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load dynamic URLs for sitemap");
        }

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(SitemapNs + "urlset",
                new XAttribute(XNamespace.Xmlns + "xhtml", XhtmlNs),
                urls));

        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + document;
    }

    public string GenerateRobotsTxt()
    {
        var baseUrl = GetBaseUrl();
        var builder = new StringBuilder();
        builder.AppendLine("User-agent: AhrefsBot");
        builder.AppendLine("Crawl-delay: 10");
        builder.AppendLine();
        builder.AppendLine("User-agent: SemrushBot");
        builder.AppendLine("Crawl-delay: 10");
        builder.AppendLine();
        builder.AppendLine("User-agent: *");
        builder.AppendLine("Allow: /");
        builder.AppendLine("Disallow: /admin/");
        builder.AppendLine("Disallow: /account/");
        builder.AppendLine("Disallow: /auth/");
        builder.AppendLine("Disallow: /checkout");
        builder.AppendLine("Disallow: /cart");
        builder.AppendLine();
        builder.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
        return builder.ToString();
    }

    private string GetBaseUrl()
    {
        var configured = _configuration["SiteSettings:BaseUrl"]
            ?? _configuration["App:FrontendBaseUrl"]
            ?? "https://idealweightnutrition.ae";

        return configured.TrimEnd('/').Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
    }

    private XElement CreateUrl(string url, DateTime lastModified, string changeFrequency, double priority)
    {
        var element = new XElement(SitemapNs + "url",
            new XElement(SitemapNs + "loc", url),
            new XElement(SitemapNs + "lastmod", lastModified.ToString("yyyy-MM-dd")),
            new XElement(SitemapNs + "changefreq", changeFrequency),
            new XElement(SitemapNs + "priority", priority.ToString("F1")));

        var separator = url.Contains('?') ? "&" : "?";
        element.Add(new XElement(XhtmlNs + "link",
            new XAttribute("rel", "alternate"),
            new XAttribute("hreflang", "ar"),
            new XAttribute("href", $"{url}{separator}lang=ar")));
        element.Add(new XElement(XhtmlNs + "link",
            new XAttribute("rel", "alternate"),
            new XAttribute("hreflang", "en"),
            new XAttribute("href", $"{url}{separator}lang=en")));

        return element;
    }
}
