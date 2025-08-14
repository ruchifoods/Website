namespace FoodDeliveryApp.ViewModels.RestaurantOwners
{
    public class RestaurantOwnerAccountViewModel
    {
        // Basic User Details
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        // Address Details (if any)
        public int? UserAddressId { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Landmark { get; set; }
        // Business-Details
        public string? BusinessLicenseNumber { get; set; }
        public string? GSTIN { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
    }
}