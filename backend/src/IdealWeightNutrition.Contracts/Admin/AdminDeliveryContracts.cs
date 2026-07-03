namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminCityDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public required string Emirate { get; init; }
    public string? EmirateAr { get; init; }
    public required double DeliveryCharge { get; init; }
    public required bool IsActive { get; init; }
    public required int DisplayOrder { get; init; }
    public required int RemoteAreaCount { get; init; }
}

public sealed class AdminRemoteAreaDto
{
    public required int Id { get; init; }
    public required int CityId { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public required double DeliveryCharge { get; init; }
    public required bool IsActive { get; init; }
    public required int DisplayOrder { get; init; }
}

public sealed class UpsertAdminCityRequest
{
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public required string Emirate { get; init; }
    public string? EmirateAr { get; init; }
    public double DeliveryCharge { get; init; }
    public bool IsActive { get; init; } = true;
    public int DisplayOrder { get; init; }
}

public sealed class UpsertAdminRemoteAreaRequest
{
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public double DeliveryCharge { get; init; }
    public bool IsActive { get; init; } = true;
    public int DisplayOrder { get; init; }
}
