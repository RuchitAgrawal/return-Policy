using Microsoft.EntityFrameworkCore;
using ReturnFlow.Core.Entities;
using ReturnFlow.Core.Enums;

namespace ReturnFlow.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // Ensure database schema is created
        await context.Database.EnsureCreatedAsync();

        if (await context.Products.AnyAsync())
        {
            return; // DB has already been seeded
        }

        var now = DateTime.UtcNow;

        // ── 1. Products ──────────────────────────────────────────────────────
        var p1 = new Product
        {
            Sku = "SKU-MECH-KB01",
            Name = "Keychron Pro Wireless Mechanical Keyboard",
            Category = "Peripherals",
            UnitPrice = 9500.00m,
            WarrantyDays = 365,
            CurrentStockOnHand = 42,
            DamagedStockCount = 0
        };

        var p2 = new Product
        {
            Sku = "SKU-MON-4K27",
            Name = "UltraSharp 27-inch 4K Hub Monitor",
            Category = "Displays",
            UnitPrice = 38000.00m,
            WarrantyDays = 730,
            CurrentStockOnHand = 15,
            DamagedStockCount = 0
        };

        var p3 = new Product
        {
            Sku = "SKU-DOCK-TB4",
            Name = "Thunderbolt 4 Enterprise Docking Station",
            Category = "Hubs & Docks",
            UnitPrice = 18500.00m,
            WarrantyDays = 365,
            CurrentStockOnHand = 88,
            DamagedStockCount = 0
        };

        var p4 = new Product
        {
            Sku = "SKU-HEAD-ANC9",
            Name = "Studio ANC Wireless Headset",
            Category = "Audio",
            UnitPrice = 14200.00m,
            WarrantyDays = 180,
            CurrentStockOnHand = 64,
            DamagedStockCount = 0
        };

        var p5 = new Product
        {
            Sku = "SKU-WEBCAM-4K",
            Name = "ProStream 4K AI Webcam",
            Category = "Video",
            UnitPrice = 8900.00m,
            WarrantyDays = 365,
            CurrentStockOnHand = 120,
            DamagedStockCount = 0
        };

        context.Products.AddRange(p1, p2, p3, p4, p5);
        await context.SaveChangesAsync();

        // ── 2. Orders & Line Items ───────────────────────────────────────────
        // Order 1: ORD-2026-1001 (Delivered 5 days ago, Shopify) - Keyboard + Headset
        var o1 = new Order
        {
            OrderNumber = "ORD-2026-1001",
            CustomerName = "Aarav Sharma",
            CustomerEmail = "aarav.sharma@example.com",
            OrderDate = now.AddDays(-7),
            DeliveredDate = now.AddDays(-5),
            SalesChannel = SalesChannel.Shopify,
            TotalAmount = 23700.00m
        };
        context.Orders.Add(o1);
        await context.SaveChangesAsync();

        var li1_1 = new OrderLineItem
        {
            OrderId = o1.Id,
            ProductId = p1.Id,
            QuantityPurchased = 1,
            UnitPricePaid = 9500.00m
        };
        var li1_2 = new OrderLineItem
        {
            OrderId = o1.Id,
            ProductId = p4.Id,
            QuantityPurchased = 1,
            UnitPricePaid = 14200.00m
        };
        context.OrderLineItems.AddRange(li1_1, li1_2);

        // Order 2: ORD-2026-1002 (Delivered 22 days ago, AmazonIN) - 4K Monitor
        var o2 = new Order
        {
            OrderNumber = "ORD-2026-1002",
            CustomerName = "Priya Patel",
            CustomerEmail = "priya.p@example.com",
            OrderDate = now.AddDays(-25),
            DeliveredDate = now.AddDays(-22),
            SalesChannel = SalesChannel.AmazonIN,
            TotalAmount = 38000.00m
        };
        context.Orders.Add(o2);
        await context.SaveChangesAsync();

        var li2_1 = new OrderLineItem
        {
            OrderId = o2.Id,
            ProductId = p2.Id,
            QuantityPurchased = 1,
            UnitPricePaid = 38000.00m
        };
        context.OrderLineItems.Add(li2_1);

        // Order 3: ORD-2026-1003 (Delivered 75 days ago, B2BWholesale) - 10x Docks
        var o3 = new Order
        {
            OrderNumber = "ORD-2026-1003",
            CustomerName = "TechCorp Solutions India",
            CustomerEmail = "procurement@techcorp.in",
            OrderDate = now.AddDays(-80),
            DeliveredDate = now.AddDays(-75),
            SalesChannel = SalesChannel.B2BWholesale,
            TotalAmount = 185000.00m
        };
        context.Orders.Add(o3);
        await context.SaveChangesAsync();

        var li3_1 = new OrderLineItem
        {
            OrderId = o3.Id,
            ProductId = p3.Id,
            QuantityPurchased = 10,
            UnitPricePaid = 18500.00m
        };
        context.OrderLineItems.Add(li3_1);

        // Order 4: ORD-2026-1004 (Delivered 410 days ago, DirectWeb) - Webcam
        var o4 = new Order
        {
            OrderNumber = "ORD-2026-1004",
            CustomerName = "Vikram Malhotra",
            CustomerEmail = "vikram.m@example.com",
            OrderDate = now.AddDays(-415),
            DeliveredDate = now.AddDays(-410),
            SalesChannel = SalesChannel.DirectWeb,
            TotalAmount = 8900.00m
        };
        context.Orders.Add(o4);
        await context.SaveChangesAsync();

        var li4_1 = new OrderLineItem
        {
            OrderId = o4.Id,
            ProductId = p5.Id,
            QuantityPurchased = 1,
            UnitPricePaid = 8900.00m
        };
        context.OrderLineItems.Add(li4_1);

        // Order 5: ORD-2026-1005 (Delivered 2 days ago, Shopify) - 4K Monitor
        var o5 = new Order
        {
            OrderNumber = "ORD-2026-1005",
            CustomerName = "Neha Verma",
            CustomerEmail = "neha.verma@example.com",
            OrderDate = now.AddDays(-4),
            DeliveredDate = now.AddDays(-2),
            SalesChannel = SalesChannel.Shopify,
            TotalAmount = 38000.00m
        };
        context.Orders.Add(o5);
        await context.SaveChangesAsync();

        var li5_1 = new OrderLineItem
        {
            OrderId = o5.Id,
            ProductId = p2.Id,
            QuantityPurchased = 1,
            UnitPricePaid = 38000.00m
        };
        context.OrderLineItems.Add(li5_1);

        await context.SaveChangesAsync();

        // ── 3. Pre-Processed RMA Tickets ──────────────────────────────────────
        // Ticket 1: AutoApproved
        var t1 = new RmaTicket
        {
            TicketNumber = "RMA-9001",
            OrderId = o1.Id,
            OrderLineItemId = li1_1.Id,
            CreatedAt = now.AddHours(-18),
            Status = RmaStatus.AutoApproved,
            CustomerComplaintRawText = "Ordered duplicate by mistake, box is completely factory sealed and unopened. Requesting full refund.",
            RequestedResolution = RequestedResolution.Refund,
            ConditionReportedByCustomer = ConditionReported.Unopened,
            DaysSinceDelivery = 5,
            IsWithinReturnWindow = true,
            IsWithinWarrantyWindow = true,
            CalculatedRefundAmount = 9500.00m,
            RestockingFeeAmount = 0.00m,
            AiExtractedDefectCategory = DefectCategory.BuyerRemorse,
            AiSentimentScore = 2,
            AiFraudRiskLevel = FraudRiskLevel.Low,
            AiRecommendedAction = RecommendedAction.ApproveFullRefund,
            WarehouseRoutingAction = WarehouseRoutingAction.RestockToSellable,
            AiReasoningSummary = "Return filed within 5 days of delivery with factory sealed condition. Eligible for standard 30-day remorse return with zero restocking fees.",
            AiDraftedCustomerEmail = "Dear Aarav Sharma,\n\nYour return request for the Keychron Pro Wireless Mechanical Keyboard has been auto-approved. A prepaid return shipping label has been dispatched to your email. Your full refund of ₹9,500.00 will be credited once received."
        };

        // Ticket 2: PendingReview
        var t2 = new RmaTicket
        {
            TicketNumber = "RMA-9002",
            OrderId = o3.Id,
            OrderLineItemId = li3_1.Id,
            CreatedAt = now.AddHours(-6),
            Status = RmaStatus.PendingReview,
            CustomerComplaintRawText = "The Thunderbolt dock keeps disconnecting every 10 minutes and smells like burnt plastic. Need an urgent replacement for our office workstation.",
            RequestedResolution = RequestedResolution.Replacement,
            ConditionReportedByCustomer = ConditionReported.Defective,
            DaysSinceDelivery = 75,
            IsWithinReturnWindow = false,
            IsWithinWarrantyWindow = true,
            CalculatedRefundAmount = 18500.00m,
            RestockingFeeAmount = 0.00m,
            AiExtractedDefectCategory = DefectCategory.HardwareDefect,
            AiSentimentScore = 7,
            AiFraudRiskLevel = FraudRiskLevel.Low,
            AiRecommendedAction = RecommendedAction.ApproveReplacement,
            WarehouseRoutingAction = WarehouseRoutingAction.SendToRefurbish,
            AiReasoningSummary = "Hardware failure confirmed with burning smell. Unit delivered 75 days ago is outside standard 30-day return window but well within the 365-day manufacturer warranty. Valid for replacement.",
            AiDraftedCustomerEmail = "Dear TechCorp Team,\n\nWe apologize for the fault with your Thunderbolt 4 Docking Station. Your warranty claim has been verified, and our engineering team is preparing an expedited replacement unit for your office."
        };

        // Ticket 3: QuarantinedForFraud
        var t3 = new RmaTicket
        {
            TicketNumber = "RMA-9003",
            OrderId = o2.Id,
            OrderLineItemId = li2_1.Id,
            CreatedAt = now.AddHours(-2),
            Status = RmaStatus.QuarantinedForFraud,
            CustomerComplaintRawText = "I dropped the 4K monitor down the stairs while moving apartments and the screen shattered. Send me a brand new one under warranty immediately or I will post bad reviews everywhere!",
            RequestedResolution = RequestedResolution.Replacement,
            ConditionReportedByCustomer = ConditionReported.Defective,
            DaysSinceDelivery = 22,
            IsWithinReturnWindow = true,
            IsWithinWarrantyWindow = true,
            CalculatedRefundAmount = 0.00m,
            RestockingFeeAmount = 0.00m,
            AiExtractedDefectCategory = DefectCategory.SuspectedAbuse,
            AiSentimentScore = 9,
            AiFraudRiskLevel = FraudRiskLevel.High,
            AiRecommendedAction = RecommendedAction.EscalateToManualInspect,
            WarehouseRoutingAction = WarehouseRoutingAction.HoldInQuarantine,
            AiReasoningSummary = "High fraud risk: customer admits user-inflicted physical damage (dropped down stairs) combined with extortion threats. Physical damage is explicitly excluded from warranty coverage.",
            AiDraftedCustomerEmail = "Dear Priya Patel,\n\nYour return request regarding order ORD-2026-1002 has been flagged for supervisor review. Accidental drop damage is not covered under the limited hardware warranty policy. Our operations team will contact you shortly."
        };

        context.RmaTickets.AddRange(t1, t2, t3);
        await context.SaveChangesAsync();
    }
}
