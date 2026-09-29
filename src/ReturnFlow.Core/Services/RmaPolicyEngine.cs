using ReturnFlow.Core.Entities;
using ReturnFlow.Core.Enums;

namespace ReturnFlow.Core.Services;

/// <summary>
/// Deterministic SQL-backed policy engine that evaluates return/warranty eligibility
/// and calculates refund amounts BEFORE the AI triage is invoked.
/// All rules here are immutable business logic — no ML, no randomness.
/// </summary>
public static class RmaPolicyEngine
{
    private const int StandardReturnWindowDays = 30;
    private const decimal StandardRestockingFeePercent = 0.15m;
    private const decimal B2BWholesaleExtraRestockingFeePercent = 0.05m;

    public sealed record PolicyResult(
        int DaysSinceDelivery,
        bool IsWithinReturnWindow,
        bool IsWithinWarrantyWindow,
        decimal CalculatedRefundAmount,
        decimal RestockingFeeAmount,
        string PolicyNote
    );

    /// <summary>
    /// Evaluates the return/warranty policy for an RMA ticket.
    /// </summary>
    /// <param name="order">The parent order with delivery date and sales channel.</param>
    /// <param name="lineItem">The specific order line item being returned.</param>
    /// <param name="product">The product, used for WarrantyDays and UnitPrice.</param>
    /// <param name="condition">Customer-reported condition of the item.</param>
    /// <param name="defectHint">
    ///   Optional: preliminary defect category hint (e.g. from keyword scan) used
    ///   to determine whether a warranty claim is eligible. Pass null to skip.
    /// </param>
    public static PolicyResult Evaluate(
        Order order,
        OrderLineItem lineItem,
        Product product,
        ConditionReported condition,
        DefectCategory? defectHint = null)
    {
        var now = DateTime.UtcNow;
        var daysSince = (int)(now - order.DeliveredDate).TotalDays;

        bool withinReturn = daysSince <= StandardReturnWindowDays;
        bool withinWarranty = daysSince <= product.WarrantyDays;

        decimal grossRefund = lineItem.UnitPricePaid * lineItem.QuantityPurchased;
        decimal restockingFee = 0m;
        string note;

        // ── Rule 1: Within 30-day return window ──────────────────────────────
        if (withinReturn)
        {
            if (condition == ConditionReported.Unopened)
            {
                // No restocking fee for factory-sealed returns
                restockingFee = 0m;
                note = "Within return window. Unopened condition — full refund eligible.";
            }
            else if (condition == ConditionReported.OpenedUnused)
            {
                // 15% restocking fee (+ 5% B2B surcharge)
                decimal feePercent = StandardRestockingFeePercent;
                if (order.SalesChannel == SalesChannel.B2BWholesale)
                    feePercent += B2BWholesaleExtraRestockingFeePercent;

                restockingFee = Math.Round(grossRefund * feePercent, 2);
                note = $"Within return window. Opened condition — {feePercent * 100:0}% restocking fee applied.";
            }
            else // Defective
            {
                // No restocking fee for defective items within return window
                restockingFee = 0m;
                note = "Within return window. Defective condition reported — full refund eligible, no restocking fee.";
            }
        }
        // ── Rule 2: Outside return window but within warranty ────────────────
        else if (withinWarranty)
        {
            bool warrantyEligibleDefect = defectHint is
                DefectCategory.HardwareDefect or
                DefectCategory.ShippingDamage or
                DefectCategory.WrongItemSent;

            if (warrantyEligibleDefect || condition == ConditionReported.Defective)
            {
                restockingFee = 0m;
                note = "Outside return window but within warranty. Eligible defect — no restocking fee. Replacement preferred.";
            }
            else
            {
                // BuyerRemorse / WearAndTear / SuspectedAbuse via warranty claim — not covered
                grossRefund = 0m;
                restockingFee = 0m;
                note = "Outside return window. Warranty does not cover buyer's remorse or non-defect claims.";
            }
        }
        // ── Rule 3: Outside both windows ─────────────────────────────────────
        else
        {
            grossRefund = 0m;
            restockingFee = 0m;
            note = "Outside both return window and warranty window. Policy: Reject.";
        }

        decimal netRefund = grossRefund - restockingFee;

        return new PolicyResult(
            DaysSinceDelivery: daysSince,
            IsWithinReturnWindow: withinReturn,
            IsWithinWarrantyWindow: withinWarranty,
            CalculatedRefundAmount: netRefund,
            RestockingFeeAmount: restockingFee,
            PolicyNote: note
        );
    }

    /// <summary>
    /// Applies an approved RMA decision to inventory counters.
    /// Call this AFTER staff approves a ticket.
    /// </summary>
    public static void ApplyInventorySync(Product product, WarehouseRoutingAction routing, int quantity)
    {
        switch (routing)
        {
            case WarehouseRoutingAction.RestockToSellable:
                product.CurrentStockOnHand += quantity;
                break;

            case WarehouseRoutingAction.SendToRefurbish:
            case WarehouseRoutingAction.ScrapDefect:
                product.DamagedStockCount += quantity;
                break;

            case WarehouseRoutingAction.HoldInQuarantine:
                // No inventory change — item held pending inspection
                break;
        }
    }
}

