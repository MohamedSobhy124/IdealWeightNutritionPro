using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminCatalogueService : IAdminCatalogueService
{
    private readonly AppDbContext _db;

    public AdminCatalogueService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminCategoryDto>> ListCategoriesAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Categories.AsNoTracking();
        if (!includeDeleted)
            query = query.Where(c => !c.IsDeleted);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => MapCategory(c))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(
        UpsertAdminCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNames(request.Name, request.NameAr);

        var category = new Category
        {
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            DescriptionAr = request.DescriptionAr?.Trim() ?? string.Empty,
            ImageUrl = request.ImageUrl?.Trim(),
            IsDeleted = false
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    public async Task<AdminCategoryDto> UpdateCategoryAsync(
        int id,
        UpsertAdminCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNames(request.Name, request.NameAr);

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Category not found.");

        category.Name = request.Name.Trim();
        category.NameAr = request.NameAr.Trim();
        category.Description = request.Description?.Trim() ?? string.Empty;
        category.DescriptionAr = request.DescriptionAr?.Trim() ?? string.Empty;
        category.ImageUrl = request.ImageUrl?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Category not found.");

        var inUse = await _db.Products.AnyAsync(p => p.CategryId == id && !p.IsDeleted, cancellationToken);
        if (inUse)
            throw new InvalidOperationException("Cannot delete a category that has active products.");

        category.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminBrandDto>> ListBrandsAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Brands.AsNoTracking();
        if (!includeDeleted)
            query = query.Where(b => !b.IsDeleted);

        return await query
            .OrderBy(b => b.Name)
            .Select(b => MapBrand(b))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminBrandDto> CreateBrandAsync(
        UpsertAdminBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNames(request.Name, request.NameAr);

        var brand = new Brand
        {
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            Description = request.Description?.Trim(),
            DescriptionAr = request.DescriptionAr?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            IsDeleted = false
        };

        _db.Brands.Add(brand);
        await _db.SaveChangesAsync(cancellationToken);
        return MapBrand(brand);
    }

    public async Task<AdminBrandDto> UpdateBrandAsync(
        int id,
        UpsertAdminBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateNames(request.Name, request.NameAr);

        var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Brand not found.");

        brand.Name = request.Name.Trim();
        brand.NameAr = request.NameAr.Trim();
        brand.Description = request.Description?.Trim();
        brand.DescriptionAr = request.DescriptionAr?.Trim();
        brand.ImageUrl = request.ImageUrl?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return MapBrand(brand);
    }

    public async Task DeleteBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        var brand = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Brand not found.");

        var inUse = await _db.Products.AnyAsync(p => p.BrandId == id && !p.IsDeleted, cancellationToken);
        if (inUse)
            throw new InvalidOperationException("Cannot delete a brand that has active products.");

        brand.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateNames(string name, string nameAr)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("English name is required.");
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new InvalidOperationException("Arabic name is required.");
    }

    private static AdminCategoryDto MapCategory(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        NameAr = c.NameAr,
        Description = c.Description,
        DescriptionAr = c.DescriptionAr,
        ImageUrl = c.ImageUrl,
        IsDeleted = c.IsDeleted
    };

    private static AdminBrandDto MapBrand(Brand b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        NameAr = b.NameAr,
        Description = b.Description,
        DescriptionAr = b.DescriptionAr,
        ImageUrl = b.ImageUrl,
        IsDeleted = b.IsDeleted
    };
}
