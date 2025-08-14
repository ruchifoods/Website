namespace FoodDeliveryApp.Models
{
    public class PaymentTypeMaster
    {
        public int PaymentTypeMasterId { get; set; }
        public string TypeName { get; set; }  // "CreditCard", "UPI", "CashOnDelivery"
        public string? Description { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<Payment> Payments { get; set; }
    }
}