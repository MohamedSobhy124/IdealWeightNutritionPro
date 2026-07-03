using IdealWeightNutrition.Contracts.Orders;
using IdealWeightNutrition.Contracts.Returns;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminOrderService
{
    Task<AdminOrderListResponse> ListOrdersAsync(
        AdminOrderQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCsvAsync(
        AdminOrderQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportProductProfitsCsvAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    Task<AdminOrderStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminOrderAuditLogDto>> GetAuditLogsAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<AdminOrderDetailDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<AdminOrderActionResponse> StartProcessingAsync(int orderId, CancellationToken cancellationToken = default);

    Task<AdminOrderActionResponse> ShipOrderAsync(
        int orderId,
        ShipOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminOrderActionResponse> MarkDeliveredAsync(int orderId, CancellationToken cancellationToken = default);

    Task<AdminOrderActionResponse> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<AdminOrderActionResponse> RecheckPaymentStatusAsync(int orderId, CancellationToken cancellationToken = default);

    Task<AdminOrderActionResponse> ForceCompleteAsync(
        int orderId,
        ForceOrderActionRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminOrderActionResponse> ForceCancelAsync(
        int orderId,
        ForceOrderActionRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminOrderActionResponse> UpdateOrderLineAsync(
        int orderId,
        UpdateOrderLineRequest request,
        CancellationToken cancellationToken = default);

    Task<RefundOrderResponse> RefundOrderAsync(
        int orderId,
        RefundOrderRequest request,
        CancellationToken cancellationToken = default);
}
