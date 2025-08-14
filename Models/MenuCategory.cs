using System.ComponentModel.DataAnnotations;

namespace FoodDeliveryApp.Models
{
    // Many restaurants group menu items under categories, e.g., Starters, Main Course, Desserts, etc.
    public class MenuCategory
    {
        public int MenuCategoryId { get; set; }
        public string CategoryName { get; set; } //Starter, Main Course
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }

        [Display(Name = "Show Schedule")]
        public bool IsActive { get; set; } = true;

        // Restaurant Relationship
        public int RestaurantId { get; set; }
        public virtual Restaurant Restaurant { get; set; }

        // Navigation
        public virtual ICollection<MenuItem> MenuItems { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}