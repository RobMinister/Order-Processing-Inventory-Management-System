using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Data;
using OrderProcessingSystem.Models;
using OrderProcessingSystem.Services;

namespace OrderProcessingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(List<OrderItem> items)
        {
            if (items == null || !items.Any())
                return BadRequest("Order must contain at least one item.");

            var productIds = items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                                         .Where(p => productIds.Contains(p.Id))
                                         .ToDictionaryAsync(p => p.Id);

            foreach (var item in items)
            {
                if (!products.ContainsKey(item.ProductId))
                    return BadRequest($"Product ID {item.ProductId} not found.");
            }

            // Lock stock updates per product
            foreach (var item in items)
            {
                var product = products[item.ProductId];
                var productLock = InventoryLockManager.GetLockForProduct(product.Id);

                lock (productLock)
                {
                    if (product.StockQuantity < item.Quantity)
                        return BadRequest($"Insufficient stock for product '{product.Name}'.");

                    product.StockQuantity -= item.Quantity;
                }
            }

            var order = new Order
            {
                Items = items
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            return Ok(new { order.Id, Status = order.Status });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                                      .Include(o => o.Items)
                                      .ThenInclude(i => i.Product)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return order;
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders
                                      .Include(o => o.Items)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound("Order not found.");
            if (order.Status != "Pending Fulfillment")
                return BadRequest("Only unfulfilled orders can be canceled.");

            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var productLock = InventoryLockManager.GetLockForProduct(product.Id);
                    lock (productLock)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }
            }

            order.Status = "Canceled";
            await _context.SaveChangesAsync();

            return Ok(new { order.Id, Status = order.Status });
        }
    }
}
