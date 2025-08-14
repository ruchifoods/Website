namespace FoodDeliveryApp.Models
{
    public class UserAddress
    {
        public int UserAddressId { get; set; }
        public int UserId { get; set; } //FK
        public virtual User User { get; set; }
        public string? Label { get; set; }         // e.g., "Home", "Work"
        public string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string? Landmark { get; set; }
        public string? Latitude { get; set; }       // optional for geo
        public string? Longitude { get; set; }      // optional for geo
    }
}