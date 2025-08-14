namespace FoodDeliveryApp.Models
{
    public class MenuItemQuantity
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }

        public int QuantityId { get; set; }
        public int SubQuantityId { get; set; }
    }
}