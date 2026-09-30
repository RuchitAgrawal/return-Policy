using ReturnFlow.Core.Interfaces;
using ReturnFlow.Core.Models;

namespace ReturnFlow.Core.Services;

/// <summary>
/// Deterministic heuristic fallback triage service.
/// Used whenever OPENAI_API_KEY is not configured, allowing reviewers
/// and hiring managers to test all workflows with zero external dependencies.
/// </summary>
public class MockAiTriageService : IAiTriageService
{
    public bool IsLiveMode => false;

    public Task<AiTriageResult> AnalyzeAsync(string complaintText, AiTriageContext context)
    {
        var lower = (complaintText ?? string.Empty).ToLowerInvariant();

        // Preset 3: Suspected Fraud / Physical Abuse / Coercion
        if (lower.Contains("dropped") || lower.Contains("stairs") || lower.Contains("shattered") ||
            lower.Contains("bad reviews") || lower.Contains("smashed") || lower.Contains("crack"))
        {
            return Task.FromResult(new AiTriageResult(
                DefectCategory: "SuspectedAbuse",
                SentimentScore: 9,
                FraudRiskLevel: "High",
                RecommendedAction: "EscalateToManualInspect",
                WarehouseRoutingAction: "HoldInQuarantine",
                ReasoningSummary: "Customer explicitly reported accidental user damage (shattered display/drop) combined with aggressive review-extortion language. Standard limited warranty excludes physical impact damage. Claim quarantined for supervisor review.",
                DraftedCustomerEmail: $"Dear {context.OrderNumber} Customer,\n\nYour return request for {context.ProductName} has been submitted and escalated to our Senior Warranty Specialists. Please note that accidental physical damage falls outside standard manufacturer warranty terms. A specialist will review your case within 1 business day."
            ));
        }

        // Preset 2: Buyer Remorse
        if (lower.Contains("don't like") || lower.Contains("dont like") || lower.Contains("switch sound") ||
            lower.Contains("changed my mind") || lower.Contains("remorse") || lower.Contains("not needed"))
        {
            decimal feePercent = context.SalesChannel == "B2BWholesale" ? 20m : 15m;
            decimal restockingFee = Math.Round(context.UnitPrice * (feePercent / 100m), 2);
            decimal netRefund = Math.Max(0m, context.UnitPrice - restockingFee);

            return Task.FromResult(new AiTriageResult(
                DefectCategory: "BuyerRemorse",
                SentimentScore: 3,
                FraudRiskLevel: "Low",
                RecommendedAction: "ApproveWithRestockingFee",
                WarehouseRoutingAction: "SendToRefurbish",
                ReasoningSummary: $"Customer requested return due to personal preference within the {context.DaysSinceDelivery}-day delivery window. In accordance with policy for opened equipment, return is approved subject to a standard {feePercent:0}% restocking fee.",
                DraftedCustomerEmail: $"Dear Customer,\n\nWe have approved your return request for the {context.ProductName}. Because the product was opened, a {feePercent:0}% restocking fee (₹{restockingFee:N2}) applies. Your estimated net refund is ₹{netRefund:N2}, which will be issued to your original payment method once the item passes warehouse inspection."
            ));
        }

        // Preset 1: Legitimate Hardware / Safety Defect
        if (lower.Contains("burnt") || lower.Contains("burning") || lower.Contains("smoke") ||
            lower.Contains("disconnecting") || lower.Contains("defective") || lower.Contains("not working") ||
            lower.Contains("hardware") || lower.Contains("malfunction"))
        {
            return Task.FromResult(new AiTriageResult(
                DefectCategory: "HardwareDefect",
                SentimentScore: 7,
                FraudRiskLevel: "Low",
                RecommendedAction: context.IsWithinWarrantyWindow ? "ApproveReplacement" : "RejectOutOfPolicy",
                WarehouseRoutingAction: "SendToRefurbish",
                ReasoningSummary: context.IsWithinWarrantyWindow
                    ? $"Legitimate hardware failure detected with potential electrical/safety risk. Order delivered {context.DaysSinceDelivery} days ago is safely within the {context.WarrantyDays}-day warranty. Direct replacement approved with expedited shipping."
                    : $"Hardware failure reported, but delivery occurred {context.DaysSinceDelivery} days ago, exceeding the {context.WarrantyDays}-day warranty window. Claim rejected out of policy.",
                DraftedCustomerEmail: context.IsWithinWarrantyWindow
                    ? $"Dear Customer,\n\nThank you for alerting us regarding the issue with your {context.ProductName}. Given the electrical defect reported, we have approved an immediate replacement under your active warranty. A pre-paid reverse pickup has been scheduled, and your replacement tracking details will follow shortly."
                    : $"Dear Customer,\n\nWe regret to inform you that your return request for {context.ProductName} cannot be processed under warranty as the {context.WarrantyDays}-day coverage period expired {context.DaysSinceDelivery - context.WarrantyDays} days ago."
            ));
        }

        // Wrong Item Sent
        if (lower.Contains("wrong item") || lower.Contains("different product") || lower.Contains("incorrect"))
        {
            return Task.FromResult(new AiTriageResult(
                DefectCategory: "WrongItemSent",
                SentimentScore: 6,
                FraudRiskLevel: "Low",
                RecommendedAction: "ApproveFullRefund",
                WarehouseRoutingAction: "RestockToSellable",
                ReasoningSummary: "Fulfillment discrepancy reported: wrong item delivered to customer. Full refund or replacement authorized immediately with no fees.",
                DraftedCustomerEmail: $"Dear Customer,\n\nWe apologize for the fulfillment error regarding order {context.OrderNumber}. We have approved a full refund of ₹{context.UnitPrice:N2} with zero restocking fees. A carrier pickup has been scheduled."
            ));
        }

        // Shipping Damage
        if (lower.Contains("shipping damage") || lower.Contains("box crushed") || lower.Contains("damaged in transit") || lower.Contains("carrier"))
        {
            return Task.FromResult(new AiTriageResult(
                DefectCategory: "ShippingDamage",
                SentimentScore: 6,
                FraudRiskLevel: "Low",
                RecommendedAction: "ApproveFullRefund",
                WarehouseRoutingAction: "SendToRefurbish",
                ReasoningSummary: "Transit/carrier damage reported. Customer not at fault; eligible for immediate resolution with freight carrier claim filed in background.",
                DraftedCustomerEmail: $"Dear Customer,\n\nWe are sorry your {context.ProductName} arrived damaged during shipping. We have approved your return for a full replacement or refund. A prepaid return shipping label has been dispatched."
            ));
        }

        // Default Generic Fallback
        return Task.FromResult(new AiTriageResult(
            DefectCategory: "WearAndTear",
            SentimentScore: 4,
            FraudRiskLevel: "Medium",
            RecommendedAction: context.IsWithinReturnWindow ? "ApproveWithRestockingFee" : "EscalateToManualInspect",
            WarehouseRoutingAction: "SendToRefurbish",
            ReasoningSummary: $"Standard claim assessment. Delivered {context.DaysSinceDelivery} days ago. Condition reported: {context.ConditionReportedByCustomer}. Forwarded for warehouse physical triage.",
            DraftedCustomerEmail: $"Dear Customer,\n\nWe have received your return inquiry for {context.ProductName} (Order: {context.OrderNumber}). Our returns desk is currently reviewing the specifications of your claim and will provide authorization instructions shortly."
        ));
    }
}
