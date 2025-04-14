namespace OrderProcessingSystem.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending Fulfillment";

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
