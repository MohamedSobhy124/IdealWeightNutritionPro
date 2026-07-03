namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminCategoryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public required string Description { get; init; }
    public required string DescriptionAr { get; init; }
    public string? ImageUrl { get; init; }
    public required bool IsDeleted { get; init; }
}

public sealed class UpsertAdminCategoryRequest
{
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public string Description { get; init; } = string.Empty;
    public string DescriptionAr { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}

public sealed class AdminBrandDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public string? Description { get; init; }
    public string? DescriptionAr { get; init; }
    public string? ImageUrl { get; init; }
    public required bool IsDeleted { get; init; }
}

public sealed class UpsertAdminBrandRequest
{
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public string? Description { get; init; }
    public string? DescriptionAr { get; init; }
    public string? ImageUrl { get; init; }
}
