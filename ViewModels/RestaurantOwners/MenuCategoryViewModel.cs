using FoodDeliveryApp.Models;

namespace FoodDeliveryApp.ViewModels.RestaurantOwners
{
    public class MenuCategoryViewModel
    {
        public MenuCategory MenuCategoryDetails { get; set; } 
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}