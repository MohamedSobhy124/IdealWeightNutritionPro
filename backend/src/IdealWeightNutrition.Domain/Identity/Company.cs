namespace IdealWeightNutrition.Domain.Identity;

public sealed class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
