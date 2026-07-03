namespace IdealWeightNutrition.Contracts.Content;

public class BlogPostSummaryDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryAr { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorAr { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public int ReadTime { get; set; }
    public string? ImageUrl { get; set; }
    public string Excerpt { get; set; } = string.Empty;
    public string ExcerptAr { get; set; } = string.Empty;
}

public sealed class BlogPostDetailDto : BlogPostSummaryDto
{
    public string Content { get; set; } = string.Empty;
    public string ContentAr { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public string? MetaDescriptionAr { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaKeywordsAr { get; set; }
    public IReadOnlyList<BlogPostSummaryDto> RelatedPosts { get; set; } = [];
}
