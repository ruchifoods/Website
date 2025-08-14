using System.ComponentModel.DataAnnotations;

namespace FoodDeliveryApp.ViewModels.Common
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email{ get; set; }
    }
}
