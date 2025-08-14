using System.ComponentModel.DataAnnotations.Schema;
namespace FoodDeliveryApp.Models
{
    public class Offer
    {
        public int OfferId { get; set; }
        public string OfferCode { get; set; }       // e.g., "FIRST50"
        public string Description { get; set; }
        [Column(TypeName = "decimal(8,2)")]
        public decimal DiscountAmount { get; set; }
        [Column(TypeName = "decimal(8,2)")]
        public decimal DiscountPercentage { get; set; }
        public bool IsPercentage { get; set; }  // True if discount is in %, False for fixed amount
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Offer Scope
        public bool IsGlobal { get; set; }  // If true, applies to all restaurants/orders

        // Nullable FK for Restaurant-Specific Offers
        public int? RestaurantId { get; set; }
        public virtual Restaurant? Restaurant { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<OrderOffer> OrderOffers { get; set; }
    }
}   