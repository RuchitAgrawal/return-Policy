using ReturnFlow.Core.Interfaces;
using ReturnFlow.Core.Services;
using Xunit;

namespace ReturnFlow.UnitTests;

public class AiTriageFallbackTests
{
    private readonly MockAiTriageService _mockService = new();

    private static AiTriageContext CreateContext(
        string orderNum,
        string sku,
        decimal unitPrice,
        int daysSinceDelivery,
        bool withinReturn,
        bool withinWarranty,
        string salesChannel = "Shopify",
        string condition = "Defective")
    {
        return new AiTriageContext(
            OrderNumber: orderNum,
            SalesChannel: salesChannel,
            ProductName: "Thunderbolt 4 Enterprise Docking Station",
            Sku: sku,
            UnitPrice: unitPrice,
            OrderDate: DateTime.UtcNow.AddDays(-(daysSinceDelivery + 2)),
            DeliveredDate: DateTime.UtcNow.AddDays(-daysSinceDelivery),
            DaysSinceDelivery: daysSinceDelivery,
            IsWithinReturnWindow: withinReturn,
            IsWithinWarrantyWindow: withinWarranty,
            WarrantyDays: 365,
            ConditionReportedByCustomer: condition,
            RequestedResolution: "Replacement"
        );
    }

    [Fact]
    public async Task AnalyzeAsync_LegitimateWarrantyDefect_IdentifiesHardwareDefectAndApproveReplacement()
    {
        var complaint = "The Thunderbolt dock keeps disconnecting every 10 minutes and smells like burnt plastic. Need an urgent replacement for our office.";
        var ctx = CreateContext("ORD-2026-1003", "SKU-DOCK-TB4", 18500.00m, 75, false, true);

        var result = await _mockService.AnalyzeAsync(complaint, ctx);

        Assert.Equal("HardwareDefect", result.DefectCategory);
        Assert.Equal("Low", result.FraudRiskLevel);
        Assert.Equal("ApproveReplacement", result.RecommendedAction);
        Assert.Equal("SendToRefurbish", result.WarehouseRoutingAction);
        Assert.InRange(result.SentimentScore, 1, 10);
        Assert.False(string.IsNullOrWhiteSpace(result.DraftedCustomerEmail));
    }

    [Fact]
    public async Task AnalyzeAsync_BuyersRemorseOpened_IdentifiesRemorseAndApproveWithRestockingFee()
    {
        var complaint = "I opened the box and tried the keyboard for a week, but I don't like the tactile switch sound. I want a full refund.";
        var ctx = CreateContext("ORD-2026-1001", "SKU-MECH-KB01", 9500.00m, 5, true, true, condition: "OpenedUnused");

        var result = await _mockService.AnalyzeAsync(complaint, ctx);

        Assert.Equal("BuyerRemorse", result.DefectCategory);
        Assert.Equal("Low", result.FraudRiskLevel);
        Assert.Equal("ApproveWithRestockingFee", result.RecommendedAction);
        Assert.Equal("SendToRefurbish", result.WarehouseRoutingAction);
        Assert.InRange(result.SentimentScore, 1, 10);
        Assert.False(string.IsNullOrWhiteSpace(result.ReasoningSummary));
    }

    [Fact]
    public async Task AnalyzeAsync_SuspectedAbuse_IdentifiesAbuseAndEscalatesToQuarantine()
    {
        var complaint = "I dropped the 4K monitor down the stairs while moving apartments and the screen shattered. Send me a brand new one under warranty immediately or I will post bad reviews!";
        var ctx = CreateContext("ORD-2026-1002", "SKU-MON-4K27", 38000.00m, 22, true, true);

        var result = await _mockService.AnalyzeAsync(complaint, ctx);

        Assert.Equal("SuspectedAbuse", result.DefectCategory);
        Assert.Equal("High", result.FraudRiskLevel);
        Assert.Equal("EscalateToManualInspect", result.RecommendedAction);
        Assert.Equal("HoldInQuarantine", result.WarehouseRoutingAction);
        Assert.True(result.SentimentScore >= 8);
        Assert.False(string.IsNullOrWhiteSpace(result.DraftedCustomerEmail));
    }

    [Fact]
    public async Task AnalyzeAsync_SentimentScores_AlwaysWithinOneToTenRange()
    {
        var complaints = new[]
        {
            "Everything is fine just wrong color.",
            "Horrible product, worst purchase ever, shattered into pieces, scam!",
            "I changed my mind."
        };

        foreach (var c in complaints)
        {
            var ctx = CreateContext("ORD-TEST", "SKU-TEST", 5000m, 10, true, true);
            var res = await _mockService.AnalyzeAsync(c, ctx);
            Assert.InRange(res.SentimentScore, 1, 10);
        }
    }

    [Fact]
    public void IsLiveMode_ReturnsFalseForMockService()
    {
        Assert.False(_mockService.IsLiveMode);
    }
}
