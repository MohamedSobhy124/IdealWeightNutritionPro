using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminUserService : IAdminUserService
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        Roles.Admin,
        Roles.Employee,
        Roles.Company,
        Roles.Customer
    };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public AdminUserService(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IReadOnlyList<AdminUserListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users.AsNoTracking()
            .OrderByDescending(u => u.Email)
            .Take(200)
            .ToListAsync(cancellationToken);

        var companyIds = users.Where(u => u.CompanyId is > 0).Select(u => u.CompanyId!.Value).Distinct().ToList();
        var companies = companyIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Companies.AsNoTracking()
                .Where(c => companyIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var items = new List<AdminUserListItemDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            companies.TryGetValue(user.CompanyId ?? 0, out var companyName);
            items.Add(new AdminUserListItemDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Name = user.Name ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                CompanyId = user.CompanyId,
                CompanyName = companyName,
                Roles = roles.ToList()
            });
        }

        return items;
    }

    public async Task<CreateAdminUserResponse> CreateAsync(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Role))
            throw new InvalidOperationException("Email, name, password, and role are required.");

        if (!AllowedRoles.Contains(request.Role))
            throw new InvalidOperationException("Invalid role.");

        if (await _userManager.FindByEmailAsync(request.Email.Trim()) is not null)
            throw new InvalidOperationException("Email is already registered.");

        int? companyId = null;
        if (string.Equals(request.Role, Roles.Company, StringComparison.OrdinalIgnoreCase))
        {
            if (request.CompanyId is not > 0)
                throw new InvalidOperationException("Company is required for Company role users.");

            var exists = await _db.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);
            if (!exists)
                throw new InvalidOperationException("Company not found.");

            companyId = request.CompanyId;
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            Name = request.Name.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            CompanyId = companyId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(result.Errors.First().Description);

        await _userManager.AddToRoleAsync(user, request.Role);

        return new CreateAdminUserResponse
        {
            UserId = user.Id,
            Message = $"{request.Role} user created successfully."
        };
    }
}
