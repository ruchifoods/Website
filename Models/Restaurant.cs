using System.ComponentModel.DataAnnotations.Schema;

namespace FoodDeliveryApp.Models
{
    public class Restaurant
    {
        public int RestaurantId { get; set; }

        // Foreign key to the "owner" if needed
        public int OwnerId { get; set; }
        public virtual User Owner { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsApproved { get; set; } // Admin approval for listing

        // Possibly store operational timings
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
        public string? LogoUrl { get; set; }  // e.g., restaurant logo image

        // Navigation
        public virtual ICollection<RestaurantCuisine> RestaurantCuisines { get; set; } //Italian, Indian, Chinse
        public virtual ICollection<MenuCategory> MenuCategories { get; set; } //Starters, Deserts, Main Courses
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<RestaurantClosure> Closures { get; set; }

        [NotMapped]
        public bool IsOpen
        {
            get
            {
                var currentTime = DateTime.Now.TimeOfDay;
                var today = DateTime.Today;

                // Check if today falls within a closure period
                bool isClosedToday = Closures?.Any(c => today >= c.StartDate.Date && today <= c.EndDate.Date) ?? false;

                if (isClosedToday)
                    return false;

                return currentTime >= OpeningTime && currentTime <= ClosingTime;
            }
        }
    }
}