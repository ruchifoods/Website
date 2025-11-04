namespace FoodDeliveryApp.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public int MenuItemId { get; set; }
        public int SelectedCategoryId { get; set; }
    }
}
