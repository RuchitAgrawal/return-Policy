using ReturnFlow.Core.Enums;

namespace ReturnFlow.Core.DTOs;

// ─── Order DTOs ────────────────────────────────────────────────────────────────

public record LineItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    int QuantityPurchased,
    decimal UnitPricePaid,
    int WarrantyDays,
    bool IsWithinReturnWindow,
    bool IsWithinWarrantyWindow,
    string WarrantyBadge   // "In Warranty" | "Expired" | "Within Return Window"
);

public record OrderSummaryDto(
    int Id,
    string OrderNumber,
    string CustomerName,
    string CustomerEmail,
    DateTime OrderDate,
    DateTime DeliveredDate,
    int DaysSinceDelivery,
    string SalesChannel,
    decimal TotalAmount,
    IReadOnlyList<LineItemDto> LineItems
);

// ─── RMA Submit ────────────────────────────────────────────────────────────────

public record RmaSubmitDto(
    string OrderNumber,
    int OrderLineItemId,
    string CustomerComplaintRawText,
    RequestedResolution RequestedResolution,
    ConditionReported ConditionReportedByCustomer
);

// ─── RMA Decision (staff action) ───────────────────────────────────────────────

public record RmaDecisionDto(
    string Action   // "Approve" | "Reject" | "ApplyRestockingFee"
);

// ─── RMA Ticket Detail ─────────────────────────────────────────────────────────

public record RmaTicketDetailDto(
    int Id,
    string TicketNumber,
    string OrderNumber,
    string CustomerName,
    string CustomerEmail,
    string ProductName,
    string Sku,
    decimal UnitPricePaid,
    int QuantityPurchased,
    string SalesChannel,
    DateTime DeliveredDate,
    int DaysSinceDelivery,
    bool IsWithinReturnWindow,
    bool IsWithinWarrantyWindow,
    string CustomerComplaintRawText,
    string RequestedResolution,
    string ConditionReportedByCustomer,
    string Status,
    // Deterministic Policy Fields
    decimal CalculatedRefundAmount,
    decimal RestockingFeeAmount,
    // AI Fields
    string? AiExtractedDefectCategory,
    int AiSentimentScore,
    string? AiFraudRiskLevel,
    string? AiRecommendedAction,
    string? AiReasoningSummary,
    string? AiDraftedCustomerEmail,
    string? WarehouseRoutingAction,
    DateTime CreatedAt
);

// ─── RMA Decision Result ───────────────────────────────────────────────────────

public record RmaDecisionResultDto(
    string TicketNumber,
    string NewStatus,
    string Message,
    string? StockUpdateMessage,
    int UpdatedStockOnHand,
    int UpdatedDamagedStock
);

// ─── RMA List Item (table row) ─────────────────────────────────────────────────

public record RmaListItemDto(
    int Id,
    string TicketNumber,
    string CustomerName,
    string ProductName,
    string Sku,
    string SalesChannel,
    int DaysSinceDelivery,
    string Status,
    string? FraudRiskLevel,
    string? RecommendedAction,
    decimal CalculatedRefundAmount,
    DateTime CreatedAt
);

// ─── Inventory DTOs ────────────────────────────────────────────────────────────

public record InventorySummaryDto(
    int ProductId,
    string Sku,
    string Name,
    string Category,
    decimal UnitPrice,
    int WarrantyDays,
    int CurrentStockOnHand,
    int DamagedStockCount,
    int TotalRmaCount,
    int ApprovedRmaCount,
    decimal ReturnRatePercent  // ApprovedRmaCount / max(TotalOrdered, 1) * 100
);

// ─── Dashboard KPI ─────────────────────────────────────────────────────────────

public record DashboardKpiDto(
    int TotalActiveRmas,
    decimal TotalValueAtRisk,
    decimal AutoResolvedPercent,
    int HighFraudAlerts,
    int StockRecovered
);

