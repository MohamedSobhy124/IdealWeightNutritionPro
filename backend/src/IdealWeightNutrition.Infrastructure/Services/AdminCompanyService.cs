using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminCompanyService : IAdminCompanyService
{
    private readonly AppDbContext _db;

    public AdminCompanyService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminCompanyDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.Companies.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(Map)
            .ToListAsync(cancellationToken);

    public async Task<AdminCompanyDto?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Companies.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(Map)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AdminCompanyDto> CreateAsync(UpsertAdminCompanyRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var company = MapToEntity(request);
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);
        return MapCompany(company);
    }

    public async Task<AdminCompanyDto> UpdateAsync(int id, UpsertAdminCompanyRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        company.Name = request.Name.Trim();
        company.StreetAddress = request.StreetAddress.Trim();
        company.City = request.City.Trim();
        company.State = request.State.Trim();
        company.PostalCode = request.PostalCode.Trim();
        company.PhoneNumber = request.PhoneNumber.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return MapCompany(company);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var inUse = await _db.Users.AnyAsync(u => u.CompanyId == id, cancellationToken);
        if (inUse)
            throw new InvalidOperationException("Cannot delete a company assigned to users.");

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        _db.Companies.Remove(company);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(UpsertAdminCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Company name is required.");
    }

    private static Company MapToEntity(UpsertAdminCompanyRequest request) => new()
    {
        Name = request.Name.Trim(),
        StreetAddress = request.StreetAddress.Trim(),
        City = request.City.Trim(),
        State = request.State.Trim(),
        PostalCode = request.PostalCode.Trim(),
        PhoneNumber = request.PhoneNumber.Trim()
    };

    private static AdminCompanyDto MapCompany(Company c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        StreetAddress = c.StreetAddress,
        City = c.City,
        State = c.State,
        PostalCode = c.PostalCode,
        PhoneNumber = c.PhoneNumber
    };

    private static readonly System.Linq.Expressions.Expression<Func<Company, AdminCompanyDto>> Map =
        c => new AdminCompanyDto
        {
            Id = c.Id,
            Name = c.Name,
            StreetAddress = c.StreetAddress,
            City = c.City,
            State = c.State,
            PostalCode = c.PostalCode,
            PhoneNumber = c.PhoneNumber
        };
}
