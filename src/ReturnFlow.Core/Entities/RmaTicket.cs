using ReturnFlow.Core.Enums;

namespace ReturnFlow.Core.Entities;

public class RmaTicket
{
    public int Id { get; set; }

    /// <summary>Unique human-readable ticket number, e.g. "RMA-9001"</summary>
    public string TicketNumber { get; set; } = string.Empty;

    // ── Foreign Keys ────────────────────────────────────────────────────────
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int OrderLineItemId { get; set; }
    public OrderLineItem OrderLineItem { get; set; } = null!;

    // ── Timestamps ──────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Lifecycle Status ────────────────────────────────────────────────────
    public RmaStatus Status { get; set; } = RmaStatus.PendingReview;

    // ── Customer-Provided Fields ─────────────────────────────────────────────
    public string CustomerComplaintRawText { get; set; } = string.Empty;
    public RequestedResolution RequestedResolution { get; set; }
    public ConditionReported ConditionReportedByCustomer { get; set; }

    // ── Deterministic SQL / Policy Engine Fields ─────────────────────────────
    /// <summary>Computed: (DateTime.UtcNow - Order.DeliveredDate).TotalDays</summary>
    public int DaysSinceDelivery { get; set; }

    /// <summary>True when DaysSinceDelivery &lt;= 30</summary>
    public bool IsWithinReturnWindow { get; set; }

    /// <summary>True when DaysSinceDelivery &lt;= Product.WarrantyDays</summary>
    public bool IsWithinWarrantyWindow { get; set; }

    /// <summary>Refund amount after restocking fee deduction (INR ₹)</summary>
    public decimal CalculatedRefundAmount { get; set; }

    /// <summary>Restocking fee amount in INR ₹ (15% of unit price when applicable)</summary>
    public decimal RestockingFeeAmount { get; set; }

    // ── AI Triage Structured Output Fields ───────────────────────────────────
    public DefectCategory? AiExtractedDefectCategory { get; set; }

    /// <summary>AI-estimated customer frustration score: 1 (calm) to 10 (extreme escalation risk)</summary>
    public int AiSentimentScore { get; set; }

    public FraudRiskLevel? AiFraudRiskLevel { get; set; }
    public RecommendedAction? AiRecommendedAction { get; set; }

    public string? AiReasoningSummary { get; set; }
    public string? AiDraftedCustomerEmail { get; set; }

    public WarehouseRoutingAction? WarehouseRoutingAction { get; set; }
}

