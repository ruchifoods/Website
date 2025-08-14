namespace FoodDeliveryApp.Models
{
    public class DeliveryStatusMaster
    {
        public int DeliveryStatusMasterId { get; set; }
        public string StatusName { get; set; }   // e.g., Assigned, PickedUp, EnRoute, Delivered
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<Delivery> Deliveries { get; set; }
    }
}