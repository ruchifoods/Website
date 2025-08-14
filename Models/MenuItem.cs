using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace FoodDeliveryApp.Models
{
    // Represents each individual dish or item in a category
    public class MenuItem
    {
        public int MenuItemId { get; set; }

        // Category Relationship
        public int MenuCategoryId { get; set; }
        public  List<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();
        public string ItemName { get; set; }
        public string? Description { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal Price { get; set; }

        // Example attributes: Veg/Non-Veg, Available/Unavailable
        public bool IsVeg { get; set; }
        public bool IsAvailable { get; set; }

        // Could store an image URL
        public string? ImageUrl { get; set; }

        //[Required(ErrorMessage = "Please select an quantity.")]
        //public string SelectedQuantity { get; set; }

        public List<MenuItemQuantity> MenuItemQuantities { get; set; } = new List<MenuItemQuantity>();
        [NotMapped]
        public List<SubQuantity> SubQuantities { get; set; } = new List<SubQuantity>();
        public bool IsDeleted { get; set; }

        public FoodType FoodType { get; set; }
        public int FoodTypeId { get; set; }
    }
}