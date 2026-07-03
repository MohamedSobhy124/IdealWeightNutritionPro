namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public bool Enabled { get; set; } = true;

    public string PhoneNumber { get; set; } = "971507700559";

    public string DefaultMessage { get; set; } = "Hello! I'm interested in your products.";

    public string DefaultMessageAr { get; set; } = "مرحباً! أنا مهتم بمنتجاتكم.";
}
