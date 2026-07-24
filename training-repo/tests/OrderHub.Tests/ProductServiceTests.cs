using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_FiltersByThreshold_AndSortsByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 9, sku: "SKU-009");
        TestSetup.AddProduct(db, stock: 3, sku: "SKU-003");
        TestSetup.AddProduct(db, stock: 10, sku: "SKU-010");

        var products = await service.GetLowStockAsync(10);

        Assert.Equal(2, products.Count);
        Assert.Equal(new[] { 3, 9 }, products.Select(p => p.StockQuantity));
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 2, sku: "SKU-ACTIVE");
        TestSetup.AddProduct(db, stock: 1, isActive: false, sku: "SKU-INACTIVE");

        var products = await service.GetLowStockAsync(10);

        Assert.Single(products);
        Assert.All(products, p => Assert.NotEqual("SKU-INACTIVE", p.Sku));
    }

    [Fact]
    public async Task GetLowStock_SoldQuantityLast30Days_ExcludesCancelledOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 4, sku: "SKU-LOW");

        var confirmedOrder = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Items =
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = 3,
                    UnitPriceSnapshot = product.UnitPrice
                }
            }
        };

        var cancelledOrder = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            Items =
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = 7,
                    UnitPriceSnapshot = product.UnitPrice
                }
            }
        };

        var oldOrder = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            Items =
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = 11,
                    UnitPriceSnapshot = product.UnitPrice
                }
            }
        };

        db.Orders.AddRange(confirmedOrder, cancelledOrder, oldOrder);
        db.SaveChanges();

        var products = await service.GetLowStockAsync(10);

        Assert.Single(products);
        Assert.Equal(3, products[0].SoldQuantityLast30Days);
    }
}
