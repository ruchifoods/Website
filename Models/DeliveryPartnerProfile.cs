using System.ComponentModel.DataAnnotations.Schema;

namespace FoodDeliveryApp.Models
{
    public class DeliveryPartnerProfile
    {
        public int DeliveryPartnerProfileId { get; set; }

        // 1:1 relationship to User
        public int UserId { get; set; }
        public virtual User User { get; set; }

        // Role-specific fields
        public string LicenseNumber { get; set; }
        public string VehicleType { get; set; }         // e.g., Bike, Car
        public string VehicleRegistrationNumber { get; set; }

        // Navigation for deliveries
        public virtual ICollection<Delivery> Deliveries { get; set; }

        // This will dynamically count the number of completed deliveries without storing it.
        [NotMapped]
        public int TotalDeliveries => Deliveries?.Count ?? 0;

        // Navigation for reviews given to this delivery partner
        public virtual ICollection<Review> Reviews { get; set; }

        [NotMapped]
        public decimal AverageRating => Reviews?.Any() ?? false ? (decimal)Reviews.Average(r => r.Rating) : 0;
    }
}