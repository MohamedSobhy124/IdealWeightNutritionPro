namespace IdealWeightNutrition.Contracts.Common;

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public sealed class PagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
