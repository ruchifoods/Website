using System.ComponentModel.DataAnnotations;
namespace FoodDeliveryApp.ViewModels.RestaurantOwners
{
    public class RestaurantOwnerBusinessDetailsViewModel
    {
        public int UserId { get; set; }

        public int RestaurantOwnerProfileId { get; set; }

        [Display(Name = "Business License Number")]
        [Required(ErrorMessage = "Business License Number is required")]
        public string BusinessLicenseNumber { get; set; }

        [Display(Name = "GSTIN")]
        public string GSTIN { get; set; }

        [Display(Name = "Business Registration Number")]
        [Required(ErrorMessage = "Business Registration Number is required")]
        public string BusinessRegistrationNumber { get; set; }
    }
}