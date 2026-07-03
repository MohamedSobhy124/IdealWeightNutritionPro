using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminProductService
{
    Task<AdminProductListResponse> ListProductsAsync(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        string filter = "active",
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailDto?> GetProductAsync(int productId, CancellationToken cancellationToken = default);

    Task<AdminProductDetailDto> UpdateProductAsync(
        int productId,
        UpdateAdminProductRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetailDto> CreateProductAsync(
        CreateAdminProductRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminProductImageDto> UploadProductImageAsync(
        int productId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<AdminProductImageDto> UploadProductInfoImageAsync(
        int productId,
        Stream fileStream,
        string fileName,
        string? imageInfo,
        CancellationToken cancellationToken = default);

    Task DeleteProductImageAsync(int productId, int imageId, CancellationToken cancellationToken = default);

    Task UpdateProductInfoImageAsync(
        int productId,
        int imageId,
        string? imageInfo,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportProductsCsvAsync(
        string filter = "all",
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<RegenerateProductSlugsResponse> RegenerateAllProductSlugsAsync(
        CancellationToken cancellationToken = default);
}
