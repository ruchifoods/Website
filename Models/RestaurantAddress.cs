namespace FoodDeliveryApp.Models
{
    public class RestaurantAddress
    {
        public int RestaurantAddressId { get; set; }
        public int RestaurantId { get; set; }
        public virtual Restaurant Restaurant { get; set; }
        public string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string? Landmark { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
    }
}