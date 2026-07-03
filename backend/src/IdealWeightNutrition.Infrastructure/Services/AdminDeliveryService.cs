using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminDeliveryService : IAdminDeliveryService
{
    private readonly AppDbContext _db;

    public AdminDeliveryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminCityDto>> ListCitiesAsync(CancellationToken cancellationToken = default)
    {
        var remoteAreaCounts = await _db.RemoteAreas
            .AsNoTracking()
            .GroupBy(r => r.CityId)
            .Select(g => new { CityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CityId, x => x.Count, cancellationToken);

        var cities = await _db.Cities
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return cities.Select(c => new AdminCityDto
        {
            Id = c.Id,
            Name = c.Name,
            NameAr = c.NameAr,
            Emirate = c.Emirate,
            EmirateAr = c.EmirateAr,
            DeliveryCharge = c.DeliveryCharge,
            IsActive = c.IsActive,
            DisplayOrder = c.DisplayOrder,
            RemoteAreaCount = remoteAreaCounts.GetValueOrDefault(c.Id)
        }).ToList();
    }

    public async Task<AdminCityDto> CreateCityAsync(
        UpsertAdminCityRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCityRequest(request);

        var city = new City
        {
            Name = request.Name.Trim(),
            NameAr = request.NameAr?.Trim(),
            Emirate = request.Emirate.Trim(),
            EmirateAr = request.EmirateAr?.Trim(),
            DeliveryCharge = request.DeliveryCharge,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        _db.Cities.Add(city);
        await _db.SaveChangesAsync(cancellationToken);
        return MapCity(city, 0);
    }

    public async Task<AdminCityDto> UpdateCityAsync(
        int id,
        UpsertAdminCityRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCityRequest(request);

        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("City not found.");

        city.Name = request.Name.Trim();
        city.NameAr = request.NameAr?.Trim();
        city.Emirate = request.Emirate.Trim();
        city.EmirateAr = request.EmirateAr?.Trim();
        city.DeliveryCharge = request.DeliveryCharge;
        city.IsActive = request.IsActive;
        city.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync(cancellationToken);

        var remoteAreaCount = await _db.RemoteAreas.CountAsync(r => r.CityId == id, cancellationToken);
        return MapCity(city, remoteAreaCount);
    }

    public async Task DeleteCityAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("City not found.");

        var hasRemoteAreas = await _db.RemoteAreas.AnyAsync(r => r.CityId == id, cancellationToken);
        if (hasRemoteAreas)
            throw new InvalidOperationException("Cannot delete a city that has remote areas. Remove remote areas first or deactivate the city.");

        var usedInOrders = await _db.OrderHeaders.AnyAsync(
            o => o.City == city.Name || (city.NameAr != null && o.City == city.NameAr),
            cancellationToken);
        if (usedInOrders)
        {
            city.IsActive = false;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        _db.Cities.Remove(city);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminRemoteAreaDto>> ListRemoteAreasAsync(
        int cityId,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Cities.AnyAsync(c => c.Id == cityId, cancellationToken))
            throw new InvalidOperationException("City not found.");

        return await _db.RemoteAreas
            .AsNoTracking()
            .Where(r => r.CityId == cityId)
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .Select(r => MapRemoteArea(r))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminRemoteAreaDto> CreateRemoteAreaAsync(
        int cityId,
        UpsertAdminRemoteAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRemoteAreaRequest(request);

        if (!await _db.Cities.AnyAsync(c => c.Id == cityId, cancellationToken))
            throw new InvalidOperationException("City not found.");

        var area = new RemoteArea
        {
            CityId = cityId,
            Name = request.Name.Trim(),
            NameAr = request.NameAr?.Trim(),
            DeliveryCharge = request.DeliveryCharge,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        _db.RemoteAreas.Add(area);
        await _db.SaveChangesAsync(cancellationToken);
        return MapRemoteArea(area);
    }

    public async Task<AdminRemoteAreaDto> UpdateRemoteAreaAsync(
        int id,
        UpsertAdminRemoteAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRemoteAreaRequest(request);

        var area = await _db.RemoteAreas.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Remote area not found.");

        area.Name = request.Name.Trim();
        area.NameAr = request.NameAr?.Trim();
        area.DeliveryCharge = request.DeliveryCharge;
        area.IsActive = request.IsActive;
        area.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync(cancellationToken);
        return MapRemoteArea(area);
    }

    public async Task DeleteRemoteAreaAsync(int id, CancellationToken cancellationToken = default)
    {
        var area = await _db.RemoteAreas.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Remote area not found.");

        var usedInOrders = await _db.OrderHeaders.AnyAsync(
            o => o.Area == area.Name || (area.NameAr != null && o.Area == area.NameAr),
            cancellationToken);
        if (usedInOrders)
        {
            area.IsActive = false;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        _db.RemoteAreas.Remove(area);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCityRequest(UpsertAdminCityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("City name is required.");
        if (string.IsNullOrWhiteSpace(request.Emirate))
            throw new InvalidOperationException("Emirate is required.");
        if (request.DeliveryCharge < 0)
            throw new InvalidOperationException("Delivery charge cannot be negative.");
    }

    private static void ValidateRemoteAreaRequest(UpsertAdminRemoteAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Remote area name is required.");
        if (request.DeliveryCharge < 0)
            throw new InvalidOperationException("Delivery charge cannot be negative.");
    }

    private static AdminCityDto MapCity(City city, int remoteAreaCount) => new()
    {
        Id = city.Id,
        Name = city.Name,
        NameAr = city.NameAr,
        Emirate = city.Emirate,
        EmirateAr = city.EmirateAr,
        DeliveryCharge = city.DeliveryCharge,
        IsActive = city.IsActive,
        DisplayOrder = city.DisplayOrder,
        RemoteAreaCount = remoteAreaCount
    };

    private static AdminRemoteAreaDto MapRemoteArea(RemoteArea area) => new()
    {
        Id = area.Id,
        CityId = area.CityId,
        Name = area.Name,
        NameAr = area.NameAr,
        DeliveryCharge = area.DeliveryCharge,
        IsActive = area.IsActive,
        DisplayOrder = area.DisplayOrder
    };
}
