using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Catalogue;
using IdealWeightNutrition.Contracts.Common;

namespace IdealWeightNutrition.Api.Features.Catalogue;

public sealed class ListProductsEndpoint : Endpoint<ProductQueryRequest, PagedResult<ProductListItemDto>>
{
    private readonly ICatalogueService _catalogue;

    public ListProductsEndpoint(ICatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Get("products");
        AllowAnonymous();
        Summary(s => s.Summary = "List products with search, filter, sort, and pagination.");
    }

    public override async Task HandleAsync(ProductQueryRequest req, CancellationToken ct)
    {
        var result = await _catalogue.ListProductsAsync(req, ct);
        await Send.OkAsync(result, ct);
    }
}

public sealed class GetProductBySlugEndpoint : EndpointWithoutRequest<ProductDetailDto>
{
    private readonly ICatalogueService _catalogue;

    public GetProductBySlugEndpoint(ICatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Get("products/{slug}");
        AllowAnonymous();
        Summary(s => s.Summary = "Get product details by SEO slug (or numeric id fallback).");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var slug = Route<string>("slug");
        var product = await _catalogue.GetProductBySlugAsync(slug ?? string.Empty, ct);
        if (product is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(product, ct);
    }
}

public sealed class ListCategoriesEndpoint : EndpointWithoutRequest<IReadOnlyList<CategoryDto>>
{
    private readonly ICatalogueService _catalogue;

    public ListCategoriesEndpoint(ICatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Get("categories");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await _catalogue.ListCategoriesAsync(ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class ListBrandsEndpoint : EndpointWithoutRequest<IReadOnlyList<BrandDto>>
{
    private readonly ICatalogueService _catalogue;

    public ListBrandsEndpoint(ICatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Get("brands");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await _catalogue.ListBrandsAsync(ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class ListDiscountedProductsRequest
{
    public int Limit { get; init; } = 20;
}

public sealed class ListDiscountedProductsEndpoint : Endpoint<ListDiscountedProductsRequest, IReadOnlyList<ProductListItemDto>>
{
    private readonly ICatalogueService _catalogue;

    public ListDiscountedProductsEndpoint(ICatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Get("catalogue/discounted-products");
        AllowAnonymous();
        Summary(s => s.Summary = "List products on sale (list price above current price), ordered by discount %.");
    }

    public override async Task HandleAsync(ListDiscountedProductsRequest req, CancellationToken ct)
    {
        var items = await _catalogue.ListDiscountedProductsAsync(req.Limit, ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class GetCategoryProductSectionsRequest
{
    public int MaxCategories { get; init; } = 6;
    public int ProductsPerCategory { get; init; } = 4;
}

public sealed class GetCategoryProductSectionsEndpoint : Endpoint<GetCategoryProductSectionsRequest, IReadOnlyList<CategoryProductSectionDto>>
{
    private readonly ICatalogueService _catalogue;

    public GetCategoryProductSectionsEndpoint(ICatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Get("catalogue/category-sections");
        AllowAnonymous();
        Summary(s => s.Summary = "Homepage category tabs: top categories each with a few in-stock products.");
    }

    public override async Task HandleAsync(GetCategoryProductSectionsRequest req, CancellationToken ct)
    {
        var sections = await _catalogue.GetCategoryProductSectionsAsync(
            req.MaxCategories,
            req.ProductsPerCategory,
            ct);
        await Send.OkAsync(sections, ct);
    }
}
