using System.Text.RegularExpressions;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Content;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminBlogService : IAdminBlogService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly LegacyImageStorage _images;

    public AdminBlogService(AppDbContext db, IDateTimeProvider clock, LegacyImageStorage images)
    {
        _db = db;
        _clock = clock;
        _images = images;
    }

    public async Task<IReadOnlyList<AdminBlogPostListItemDto>> ListAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var query = _db.BlogPosts.AsNoTracking();
        if (!includeDeleted)
            query = query.Where(b => !b.IsDeleted);

        return await query
            .OrderByDescending(b => b.PublishedDate)
            .Select(b => new AdminBlogPostListItemDto
            {
                Id = b.Id,
                Slug = b.Slug,
                Title = b.Title,
                TitleAr = b.TitleAr,
                Category = b.Category,
                CategoryAr = b.CategoryAr,
                Author = b.Author,
                PublishedDate = b.PublishedDate,
                IsDeleted = b.IsDeleted,
                CreatedDate = b.CreatedDate,
                ModifiedDate = b.ModifiedDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminBlogPostDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return post is null ? null : MapDetail(post);
    }

    public async Task<AdminBlogPostDetailDto> CreateAsync(
        UpsertAdminBlogPostRequest request,
        string? createdBy,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var slug = await ResolveUniqueSlugAsync(request.Slug, request.Title, excludeId: null, cancellationToken);
        var now = _clock.Now;

        var post = new BlogPost
        {
            Slug = slug,
            Title = request.Title.Trim(),
            TitleAr = request.TitleAr.Trim(),
            Category = request.Category.Trim(),
            CategoryAr = request.CategoryAr.Trim(),
            Author = request.Author.Trim(),
            AuthorAr = request.AuthorAr.Trim(),
            PublishedDate = request.PublishedDate,
            ReadTime = request.ReadTime,
            ImageUrl = request.ImageUrl?.Trim(),
            Excerpt = request.Excerpt.Trim(),
            ExcerptAr = request.ExcerptAr.Trim(),
            Content = request.Content.Trim(),
            ContentAr = request.ContentAr.Trim(),
            MetaDescription = request.MetaDescription?.Trim(),
            MetaDescriptionAr = request.MetaDescriptionAr?.Trim(),
            MetaKeywords = request.MetaKeywords?.Trim(),
            MetaKeywordsAr = request.MetaKeywordsAr?.Trim(),
            IsDeleted = false,
            CreatedDate = now,
            CreatedBy = createdBy
        };

        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(post);
    }

    public async Task<AdminBlogPostDetailDto> UpdateAsync(
        int id,
        UpsertAdminBlogPostRequest request,
        string? modifiedBy,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var post = await _db.BlogPosts.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Blog post not found.");

        post.Slug = await ResolveUniqueSlugAsync(request.Slug, request.Title, id, cancellationToken);
        post.Title = request.Title.Trim();
        post.TitleAr = request.TitleAr.Trim();
        post.Category = request.Category.Trim();
        post.CategoryAr = request.CategoryAr.Trim();
        post.Author = request.Author.Trim();
        post.AuthorAr = request.AuthorAr.Trim();
        post.PublishedDate = request.PublishedDate;
        post.ReadTime = request.ReadTime;
        post.ImageUrl = request.ImageUrl?.Trim();
        post.Excerpt = request.Excerpt.Trim();
        post.ExcerptAr = request.ExcerptAr.Trim();
        post.Content = request.Content.Trim();
        post.ContentAr = request.ContentAr.Trim();
        post.MetaDescription = request.MetaDescription?.Trim();
        post.MetaDescriptionAr = request.MetaDescriptionAr?.Trim();
        post.MetaKeywords = request.MetaKeywords?.Trim();
        post.MetaKeywordsAr = request.MetaKeywordsAr?.Trim();
        post.ModifiedDate = _clock.Now;
        post.ModifiedBy = modifiedBy;

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(post);
    }

    public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Blog post not found.");

        post.IsDeleted = true;
        post.ModifiedDate = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Blog post not found.");

        post.IsDeleted = false;
        post.ModifiedDate = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminImageUploadResultDto> UploadImageAsync(
        int id,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Blog post not found.");

        var previousUrl = post.ImageUrl;
        var imageUrl = await _images.SaveAsync(LegacyMediaFolder.Blogs, fileStream, fileName, cancellationToken);
        post.ImageUrl = imageUrl;
        post.ModifiedDate = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(previousUrl) && !string.Equals(previousUrl, imageUrl, StringComparison.OrdinalIgnoreCase))
            _images.DeleteIfExists(previousUrl);

        return new AdminImageUploadResultDto { ImageUrl = imageUrl };
    }

    private static void ValidateRequest(UpsertAdminBlogPostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Title is required.");
        if (string.IsNullOrWhiteSpace(request.TitleAr))
            throw new InvalidOperationException("Arabic title is required.");
        if (string.IsNullOrWhiteSpace(request.Category))
            throw new InvalidOperationException("Category is required.");
        if (string.IsNullOrWhiteSpace(request.CategoryAr))
            throw new InvalidOperationException("Arabic category is required.");
        if (string.IsNullOrWhiteSpace(request.Author))
            throw new InvalidOperationException("Author is required.");
        if (string.IsNullOrWhiteSpace(request.AuthorAr))
            throw new InvalidOperationException("Arabic author is required.");
        if (string.IsNullOrWhiteSpace(request.Excerpt))
            throw new InvalidOperationException("Excerpt is required.");
        if (string.IsNullOrWhiteSpace(request.ExcerptAr))
            throw new InvalidOperationException("Arabic excerpt is required.");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new InvalidOperationException("Content is required.");
        if (string.IsNullOrWhiteSpace(request.ContentAr))
            throw new InvalidOperationException("Arabic content is required.");
        if (request.ReadTime <= 0)
            throw new InvalidOperationException("Read time must be at least 1 minute.");
    }

    private async Task<string> ResolveUniqueSlugAsync(
        string? requestedSlug,
        string title,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var baseSlug = string.IsNullOrWhiteSpace(requestedSlug)
            ? GenerateSlug(title)
            : GenerateSlug(requestedSlug);

        if (string.IsNullOrWhiteSpace(baseSlug))
            throw new InvalidOperationException("A valid slug could not be generated from the title.");

        var slug = baseSlug;
        var suffix = 2;
        while (await _db.BlogPosts.AnyAsync(
            b => b.Slug == slug && (excludeId == null || b.Id != excludeId),
            cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static string GenerateSlug(string value)
    {
        var slug = value.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", " ").Trim();
        slug = Regex.Replace(slug, @"\s", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }

    private static AdminBlogPostDetailDto MapDetail(BlogPost post) => new()
    {
        Id = post.Id,
        Slug = post.Slug,
        Title = post.Title,
        TitleAr = post.TitleAr,
        Category = post.Category,
        CategoryAr = post.CategoryAr,
        Author = post.Author,
        AuthorAr = post.AuthorAr,
        PublishedDate = post.PublishedDate,
        ReadTime = post.ReadTime,
        ImageUrl = post.ImageUrl,
        Excerpt = post.Excerpt,
        ExcerptAr = post.ExcerptAr,
        Content = post.Content,
        ContentAr = post.ContentAr,
        MetaDescription = post.MetaDescription,
        MetaDescriptionAr = post.MetaDescriptionAr,
        MetaKeywords = post.MetaKeywords,
        MetaKeywordsAr = post.MetaKeywordsAr,
        IsDeleted = post.IsDeleted,
        CreatedDate = post.CreatedDate,
        ModifiedDate = post.ModifiedDate
    };
}
