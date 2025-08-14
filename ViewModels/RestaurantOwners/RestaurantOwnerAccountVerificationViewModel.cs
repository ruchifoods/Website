namespace FoodDeliveryApp.ViewModels.RestaurantOwners
{
    public class RestaurantOwnerAccountVerificationViewModel
    {
        public string Email { get; set; }
        public bool IsVerified { get; set; }
        // Estimated time or message for verification completion
        public string EstimatedVerificationTimemessage { get; set; }
        // Display admin remarks if verification failed
        public string Message { get; set; }
    }
}
