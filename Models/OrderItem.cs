using System.ComponentModel.DataAnnotations.Schema;
namespace FoodDeliveryApp.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        public int MenuItemId { get; set; }
        public virtual MenuItem MenuItem { get; set; }

        public string Quantity { get; set; }
        [Column(TypeName = "decimal(8,2)")]
        public decimal UnitPrice { get; set; }  // price at time of ordering
        public decimal TotalPrice { get; set; }
        public int SubQuantity { get; set; }
    }
}