namespace FoodDeliveryApp.Models
{
    // Links a Restaurant with multiple Cuisine entries.
    public class RestaurantCuisine
    {
        public int RestaurantId { get; set; }
        public virtual Restaurant Restaurant { get; set; }

        public int CuisineId { get; set; }
        public virtual Cuisine Cuisine { get; set; }
    }
}