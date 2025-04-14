using Microsoft.EntityFrameworkCore;
using OrderProcessingSystem.Data;

namespace OrderProcessingSystem.Services
{
    public class OrderFulfillmentService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderFulfillmentService> _logger;

        public OrderFulfillmentService(IServiceScopeFactory scopeFactory, ILogger<OrderFulfillmentService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order fulfillment service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pendingOrders = await db.Orders
                    .Where(o => o.Status == "Pending Fulfillment")
                    .Include(o => o.Items)
                    .ToListAsync(stoppingToken);

                foreach (var order in pendingOrders)
                {
                    _logger.LogInformation($"Processing Order ID {order.Id}...");

                    await Task.Delay(2000, stoppingToken); // Simulate processing time

                    bool notificationSent = false;
                    int retryCount = 0;

                    while (!notificationSent && retryCount < 3)
                    {
                        try
                        {
                            // Simulate a flaky external notification with a 30% failure chance
                            if (new Random().NextDouble() < 0.3)
                                throw new Exception("Simulated notification failure.");

                            _logger.LogInformation($"[NOTIFY] Order {order.Id} fulfilled at {DateTime.UtcNow}");
                            notificationSent = true;
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            _logger.LogWarning($"Notification attempt {retryCount} failed for Order {order.Id}: {ex.Message}");
                            await Task.Delay(1000, stoppingToken); // Wait before retry
                        }
                    }

                    if (notificationSent)
                    {
                        order.Status = "Fulfilled";
                    }
                    else
                    {
                        _logger.LogError($"Failed to notify for Order {order.Id} after 3 retries.");
                        order.Status = "Fulfillment Failed";
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(5000, stoppingToken); // Wait before next scan
            }

            _logger.LogInformation("Order fulfillment service is stopping.");
        }
    }
}
