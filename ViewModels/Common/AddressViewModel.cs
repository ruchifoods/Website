using System.ComponentModel.DataAnnotations;
namespace FoodDeliveryApp.ViewModels.Common
{
    public class AddressViewModel
    {
        [Display(Name = "Address Type")]      
        public string? Label { get; set; }

        [Required(ErrorMessage = "Address Line 1 is required.")]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; }
        [Required(ErrorMessage = "State is required.")]
        public string State { get; set; }

        [Required(ErrorMessage = "Zip Code is required.")]
        [Display(Name = "Zip Code")]
        public string ZipCode { get; set; }

        [Display(Name = "Landmark (optional)")]
        public string? Landmark { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
    }
}