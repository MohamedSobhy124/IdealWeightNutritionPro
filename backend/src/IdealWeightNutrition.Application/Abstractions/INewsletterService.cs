using IdealWeightNutrition.Contracts.Newsletter;

namespace IdealWeightNutrition.Application.Abstractions;

public interface INewsletterService
{
    Task<NewsletterSubscribeResponse> SubscribeAsync(
        string email,
        string? source,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsletterSubscriptionDto>> ListSubscriptionsAsync(
        string? status,
        CancellationToken cancellationToken = default);

    Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<byte[]> ExportActiveCsvAsync(CancellationToken cancellationToken = default);

    Task<NewsletterStatusResponse> GetStatusAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<NewsletterUnsubscribeResponse> UnsubscribeAsync(
        string email,
        CancellationToken cancellationToken = default);
}
