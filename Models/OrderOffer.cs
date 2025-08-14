using System.ComponentModel.DataAnnotations.Schema;
namespace FoodDeliveryApp.Models
{
    public class OrderOffer
    {
        public int OrderOfferId { get; set; }

        // Foreign Keys
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        public int OfferId { get; set; }
        public virtual Offer Offer { get; set; }

        // Actual discount applied (to store historical values)
        [Column(TypeName = "decimal(8,2)")]
        public decimal DiscountApplied { get; set; }
    }
}