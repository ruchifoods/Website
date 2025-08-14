namespace FoodDeliveryApp.Models;

public class MenuCategoryItemsList
{
    public List<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();
    public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public List<Quantity> Quantities { get; set; } = new List<Quantity>();
    public List<FoodType> FoodTypes { get; set; } = new List<FoodType>();
}
