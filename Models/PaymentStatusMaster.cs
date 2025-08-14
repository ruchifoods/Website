namespace FoodDeliveryApp.Models
{
    public class PaymentStatusMaster
    {
        public int PaymentStatusMasterId { get; set; }
        public string StatusName { get; set; } // "Pending", "Completed", "Failed"
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<Payment> Payments { get; set; }
    }
}