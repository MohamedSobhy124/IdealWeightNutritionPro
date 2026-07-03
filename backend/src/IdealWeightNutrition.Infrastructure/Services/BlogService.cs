using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Content;
using IdealWeightNutrition.Domain.Content;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class BlogService : IBlogService
{
    private readonly AppDbContext _db;

    public BlogService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BlogPostSummaryDto>> ListPublishedAsync(
        CancellationToken cancellationToken = default)
    {
        var posts = await _db.BlogPosts
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.PublishedDate)
            .ToListAsync(cancellationToken);

        return posts.Select(MapSummary).ToList();
    }

    public async Task<BlogPostDetailDto?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var post = await _db.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Slug == slug && !b.IsDeleted, cancellationToken);

        if (post is null)
            return null;

        var related = await _db.BlogPosts
            .AsNoTracking()
            .Where(b =>
                !b.IsDeleted &&
                b.Id != post.Id &&
                (b.Category == post.Category || b.CategoryAr == post.CategoryAr))
            .OrderByDescending(b => b.PublishedDate)
            .Take(3)
            .ToListAsync(cancellationToken);

        return new BlogPostDetailDto
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
            RelatedPosts = related.Select(MapSummary).ToList()
        };
    }

    private static BlogPostSummaryDto MapSummary(BlogPost post) => new()
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
        ExcerptAr = post.ExcerptAr
    };
}
