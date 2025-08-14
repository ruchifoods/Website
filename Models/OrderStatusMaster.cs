namespace FoodDeliveryApp.Models
{
    public class OrderStatusMaster
    {
        public int OrderStatusMasterId { get; set; }
        public string StatusName { get; set; }      // "Placed", "Confirmed", "Preparing", "OutForDelivery", "Delivered", "Cancelled"
        public string? Description { get; set; }    // optional
        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<Order> Orders { get; set; }
    }
}