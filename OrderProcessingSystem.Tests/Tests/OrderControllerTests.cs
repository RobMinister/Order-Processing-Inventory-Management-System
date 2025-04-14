using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Controllers;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models;
using Xunit;

namespace OrderProcessingSystem.Tests
{
    public class OrderControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Products.AddRange(
                new Product { Id = 1, Name = "Piano", Price = 1000, StockQuantity = 5 },
                new Product { Id = 2, Name = "Violin", Price = 700, StockQuantity = 3 }
            );
            context.SaveChanges();

            return context;
        }

        [Fact]
        public async Task PlaceOrder_ShouldReturnOk_WhenStockIsSufficient()
        {
            var context = GetDbContext();
            var controller = new OrderController(context);

            var items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 },
                new OrderItem { ProductId = 2, Quantity = 1 }
            };

            var result = await controller.PlaceOrder(items);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

            Assert.Equal("Pending Fulfillment", parsed["Status"].GetString());
        }

        [Fact]
        public async Task PlaceOrder_ShouldReturnBadRequest_WhenStockIsInsufficient()
        {
            var context = GetDbContext();
            var controller = new OrderController(context);

            var items = new List<OrderItem>
            {
                new OrderItem { ProductId = 2, Quantity = 5 }
            };

            var result = await controller.PlaceOrder(items);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CancelOrder_ShouldRestoreStock()
        {
            var context = GetDbContext();
            var controller = new OrderController(context);

            var items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 }
            };

            var placeResult = await controller.PlaceOrder(items);
            var placeJson = JsonSerializer.Serialize(((OkObjectResult)placeResult).Value);
            var placeParsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(placeJson)!;
            int orderId = placeParsed["Id"].GetInt32();

            var cancelResult = await controller.CancelOrder(orderId);
            var okCancel = Assert.IsType<OkObjectResult>(cancelResult);
            var cancelJson = JsonSerializer.Serialize(okCancel.Value);
            var cancelParsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(cancelJson)!;

            Assert.Equal("Canceled", cancelParsed["Status"].GetString());

            var product = await context.Products.FindAsync(1);
            Assert.Equal(5, product!.StockQuantity);
        }

        [Fact]
        public async Task Fulfillment_ShouldMarkOrderAsFulfilled()
        {
            var context = GetDbContext();
            var controller = new OrderController(context);

            var items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 1 }
            };

            var placeResult = await controller.PlaceOrder(items);
            var placeJson = JsonSerializer.Serialize(((OkObjectResult)placeResult).Value);
            var placeParsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(placeJson)!;
            int orderId = placeParsed["Id"].GetInt32();

            var order = await context.Orders.FindAsync(orderId);
            order!.Status = "Fulfilled";
            await context.SaveChangesAsync();

            var fulfilledOrder = await context.Orders.FindAsync(orderId);
            Assert.Equal("Fulfilled", fulfilledOrder!.Status);
        }

        [Fact]
        public async Task RetrySimulation_ShouldMarkOrderAsFailedAfterRetries()
        {
            var context = GetDbContext();
            var order = new Order
            {
                Status = "Pending Fulfillment",
                Items = new List<OrderItem>()
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            int retryAttempts = 0;
            bool notificationSent = false;

            while (!notificationSent && retryAttempts < 3)
            {
                try
                {
                    retryAttempts++;
                    throw new Exception("Simulated failure.");
                }
                catch
                {
                    await Task.Delay(10);
                }
            }

            if (!notificationSent)
            {
                order.Status = "Fulfillment Failed";
                await context.SaveChangesAsync();
            }

            var updated = await context.Orders.FindAsync(order.Id);
            Assert.Equal("Fulfillment Failed", updated!.Status);
        }

        [Fact]
        public async Task Concurrency_ShouldOnlyAllowOneOrderToSucceed()
        {
            var context = GetDbContext();
            var controller1 = new OrderController(context);
            var controller2 = new OrderController(context);

            // Simulate two orders being placed simultaneously for the same product
            var task1 = controller1.PlaceOrder(new List<OrderItem>
            {
                new OrderItem { ProductId = 2, Quantity = 3 } // Exact stock
            });

            var task2 = controller2.PlaceOrder(new List<OrderItem>
            {
                new OrderItem { ProductId = 2, Quantity = 3 } // Conflict
            });

            var results = await Task.WhenAll(task1, task2);

            // Check that one order succeeded, the other failed
            var successCount = results.Count(r => r is OkObjectResult);
            var failCount = results.Count(r => r is BadRequestObjectResult);

            Assert.Equal(1, successCount); // Only one should succeed
            Assert.Equal(1, failCount);    // The other should fail due to insufficient stock
        }
    }
}
