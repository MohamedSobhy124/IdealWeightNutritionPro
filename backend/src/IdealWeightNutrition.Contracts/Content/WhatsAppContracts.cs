namespace IdealWeightNutrition.Contracts.Content;

public sealed class WhatsAppSettingsDto
{
    public required bool Enabled { get; init; }
    public required string PhoneNumber { get; init; }
    public required string DefaultMessage { get; init; }
    public required string DefaultMessageAr { get; init; }
}
