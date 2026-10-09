using ReturnFlow.Core.Entities;
using ReturnFlow.Core.Enums;
using ReturnFlow.Core.Services;
using Xunit;

namespace ReturnFlow.UnitTests;

public class InventoryRoutingTests
{
    [Fact]
    public void ApplyInventorySync_RestockToSellable_IncrementsCurrentStockOnHand()
    {
        var product = new Product
        {
            Sku = "SKU-MECH-KB01",
            CurrentStockOnHand = 42,
            DamagedStockCount = 0
        };

        RmaPolicyEngine.ApplyInventorySync(product, WarehouseRoutingAction.RestockToSellable, 2);

        Assert.Equal(44, product.CurrentStockOnHand);
        Assert.Equal(0, product.DamagedStockCount);
    }

    [Fact]
    public void ApplyInventorySync_SendToRefurbish_IncrementsDamagedStockCount()
    {
        var product = new Product
        {
            Sku = "SKU-DOCK-TB4",
            CurrentStockOnHand = 88,
            DamagedStockCount = 3
        };

        RmaPolicyEngine.ApplyInventorySync(product, WarehouseRoutingAction.SendToRefurbish, 1);

        Assert.Equal(88, product.CurrentStockOnHand);
        Assert.Equal(4, product.DamagedStockCount);
    }

    [Fact]
    public void ApplyInventorySync_ScrapDefect_IncrementsDamagedStockCount()
    {
        var product = new Product
        {
            Sku = "SKU-HEAD-ANC9",
            CurrentStockOnHand = 64,
            DamagedStockCount = 2
        };

        RmaPolicyEngine.ApplyInventorySync(product, WarehouseRoutingAction.ScrapDefect, 1);

        Assert.Equal(64, product.CurrentStockOnHand);
        Assert.Equal(3, product.DamagedStockCount);
    }

    [Fact]
    public void ApplyInventorySync_HoldInQuarantine_DoesNotModifyCounters()
    {
        var product = new Product
        {
            Sku = "SKU-MON-4K27",
            CurrentStockOnHand = 15,
            DamagedStockCount = 0
        };

        RmaPolicyEngine.ApplyInventorySync(product, WarehouseRoutingAction.HoldInQuarantine, 1);

        Assert.Equal(15, product.CurrentStockOnHand);
        Assert.Equal(0, product.DamagedStockCount);
    }
}
