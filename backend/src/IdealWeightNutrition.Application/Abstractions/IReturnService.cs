using IdealWeightNutrition.Contracts.Returns;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IReturnService
{
    Task<ReturnRequestDto> CreateReturnAsync(
        CreateReturnRequest request,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReturnListItemDto>> ListUserReturnsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ReturnRequestDto?> GetReturnAsync(
        int returnId,
        string? userId,
        string? guestEmail,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReturnListItemDto>> ListAdminReturnsAsync(
        string? status,
        CancellationToken cancellationToken = default);

    Task<ReturnRequestDto?> GetAdminReturnAsync(int returnId, CancellationToken cancellationToken = default);

    Task<ReturnActionResponse> ApproveReturnAsync(
        int returnId,
        ApproveReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<ReturnActionResponse> RejectReturnAsync(
        int returnId,
        RejectReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<ReturnActionResponse> MarkReturnReceivedAsync(int returnId, CancellationToken cancellationToken = default);

    Task<ReturnActionResponse> CompleteReturnAsync(
        int returnId,
        CompleteReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<ReturnActionResponse> CancelReturnAsync(
        int returnId,
        CancelReturnRequest request,
        CancellationToken cancellationToken = default);
}
