using System.ComponentModel.DataAnnotations.Schema;
namespace FoodDeliveryApp.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        // Customer who placed the order
        public int CustomerId { get; set; }
        public virtual User Customer { get; set; }

        // Relationship to Restaurant
        public int RestaurantId { get; set; }
        public virtual Restaurant Restaurant { get; set; }

        // Which address (UserAddress) the user selected for delivery
        public int? UserAddressId { get; set; }
        public virtual UserAddress UserAddress { get; set; }

        // Timestamps 
        public DateTime OrderDate { get; set; }

        // Status
        public int OrderStatusMasterId { get; set; }
        public virtual OrderStatusMaster OrderStatusMaster { get; set; }

        // Totals
        [Column(TypeName = "decimal(8,2)")]
        public decimal SubTotal { get; set; }
        [Column(TypeName = "decimal(8,2)")]
        public decimal DeliveryFee { get; set; }
        [Column(TypeName = "decimal(8,2)")]
        public decimal TaxAmount { get; set; }
        [Column(TypeName = "decimal(8,2)")]
        public decimal Discount { get; set; }
        [Column(TypeName = "decimal(8,2)")]
        public decimal TotalAmount { get; set; }

        // Additional instructions
        public string? Notes { get; set; }

        // Order Items
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        // Delivery Info
        public virtual Delivery Delivery { get; set; }

        // Payment Info
        public virtual Payment Payment { get; set; }

        //Multiple Offers can be applied to a single order
        public virtual ICollection<OrderOffer> OrderOffers { get; set; }
        public string PickUpTime { get; set; }
        public int CategoryId { get; set; } 
        public string CategoryName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress  { get; set; }
        public DateTime? CreationDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public string? OrderCode { get; set; }
        public string? Comments { get; set; }
    }
}