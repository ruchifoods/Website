namespace FoodDeliveryApp.Models
{
    // Many restaurants group menu items under categories, e.g., Starters, Main Course, Desserts, etc.
    public class MenuItemCategories
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public int MenuCategoryId { get; set; }
        public bool IsDeleted { get; set; }
    }
}