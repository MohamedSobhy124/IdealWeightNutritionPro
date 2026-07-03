namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminCompanyDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string StreetAddress { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
    public required string PhoneNumber { get; init; }
}

public sealed class UpsertAdminCompanyRequest
{
    public required string Name { get; init; }
    public string StreetAddress { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}

public sealed class AdminUserListItemDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public string? PhoneNumber { get; init; }
    public int? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}

public sealed class CreateAdminUserRequest
{
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string Password { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Role { get; init; }
    public int? CompanyId { get; init; }
}

public sealed class CreateAdminUserResponse
{
    public required string UserId { get; init; }
    public required string Message { get; init; }
}

public sealed class AdminServicePurchaseStatisticsDto
{
    public required int All { get; init; }
    public required int Pending { get; init; }
    public required int Approved { get; init; }
    public required int Rejected { get; init; }
}

public sealed class AdminProductOptionValueDto
{
    public required int Id { get; init; }
    public required string Value { get; init; }
    public required string ValueAr { get; init; }
    public required int DisplayOrder { get; init; }
}

public sealed class AdminProductOptionDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public required int DisplayOrder { get; init; }
    public required IReadOnlyList<AdminProductOptionValueDto> Values { get; init; }
}

public sealed class AddAdminProductOptionRequest
{
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class AddAdminProductOptionValueRequest
{
    public required string Value { get; init; }
    public required string ValueAr { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class UpdateAdminProductVariantDetailRequest
{
    public double Price { get; init; }
    public double? ListPrice { get; init; }
    public double? Price50 { get; init; }
    public double? Price100 { get; init; }
    public int StockQuantity { get; init; }
    public int MinimumStockAlert { get; init; } = 5;
    public string? Sku { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public sealed class GenerateVariantsResponse
{
    public required int Created { get; init; }
    public required int Skipped { get; init; }
    public required string Message { get; init; }
}

public sealed class SetProductTypeRequest
{
    public required string ProductType { get; init; }
}

public sealed class AdminActionResponse
{
    public required string Message { get; init; }
}
