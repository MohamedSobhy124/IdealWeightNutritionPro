namespace IdealWeightNutrition.Domain.Engagement;

public sealed class InAppNotification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int? OrderId { get; set; }
    public int? RelatedId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Icon { get; set; } = "bi-bell";
    public string Link { get; set; } = string.Empty;
}
