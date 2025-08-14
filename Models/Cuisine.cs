namespace FoodDeliveryApp.Models
{
    //A master table for cuisines like Indian, Chinese, Italian, etc.
    public class Cuisine
    {
        public int CuisineId { get; set; }
        public string CuisineName { get; set; }

        // For icon or thumbnail if needed
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<RestaurantCuisine> RestaurantCuisines { get; set; }
    }
}