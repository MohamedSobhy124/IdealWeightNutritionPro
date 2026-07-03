namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminBlogPostListItemDto
{
    public required int Id { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string TitleAr { get; init; }
    public required string Category { get; init; }
    public required string CategoryAr { get; init; }
    public required string Author { get; init; }
    public required DateTime PublishedDate { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
}

public sealed class AdminBlogPostDetailDto
{
    public required int Id { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string TitleAr { get; init; }
    public required string Category { get; init; }
    public required string CategoryAr { get; init; }
    public required string Author { get; init; }
    public required string AuthorAr { get; init; }
    public required DateTime PublishedDate { get; init; }
    public required int ReadTime { get; init; }
    public string? ImageUrl { get; init; }
    public required string Excerpt { get; init; }
    public required string ExcerptAr { get; init; }
    public required string Content { get; init; }
    public required string ContentAr { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaDescriptionAr { get; init; }
    public string? MetaKeywords { get; init; }
    public string? MetaKeywordsAr { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
}

public sealed class UpsertAdminBlogPostRequest
{
    public string? Slug { get; init; }
    public required string Title { get; init; }
    public required string TitleAr { get; init; }
    public required string Category { get; init; }
    public required string CategoryAr { get; init; }
    public required string Author { get; init; }
    public required string AuthorAr { get; init; }
    public DateTime PublishedDate { get; init; }
    public int ReadTime { get; init; }
    public string? ImageUrl { get; init; }
    public required string Excerpt { get; init; }
    public required string ExcerptAr { get; init; }
    public required string Content { get; init; }
    public required string ContentAr { get; init; }
    public string? MetaDescription { get; init; }
    public string? MetaDescriptionAr { get; init; }
    public string? MetaKeywords { get; init; }
    public string? MetaKeywordsAr { get; init; }
}
