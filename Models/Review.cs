namespace FoodDeliveryApp.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        // The user who wrote the review
        public int UserId { get; set; }
        public virtual User User { get; set; }

        // Possibly link to either the Restaurant, a MenuItem, or a Delivery Partner
        public int? RestaurantId { get; set; }
        public virtual Restaurant Restaurant { get; set; }

        public int? MenuItemId { get; set; }
        public virtual MenuItem MenuItem { get; set; }

        public int? DeliveryPartnerProfileId { get; set; }
        public virtual DeliveryPartnerProfile? DeliveryPartnerProfile { get; set; }

        public int Rating { get; set; }    // 1 to 5
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }
}