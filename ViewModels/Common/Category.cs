namespace FoodDeliveryApp.Models
{
    public class Category
    {
        public int MenuCategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        // Restaurant Relationship
        public int RestaurantId { get; set; }
        public bool IsSelected { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}