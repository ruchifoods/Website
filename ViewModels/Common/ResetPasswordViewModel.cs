using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace FoodDeliveryApp.ViewModels.Common
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enetr a valid Email")]
        public string Email { get; set; }
        // Token received via email (in a real-world scenario, this should be validated against a stored toke
        [Required(ErrorMessage = "Token is required")]

        public string Token { get; set; }

        [Display(Name = "New Password")]
        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character."
            )]
        public string NewPassword { get; set; }
        [Display(Name = "Confirm New Password")]
        [Required(ErrorMessage = "Confirm new password is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; }
    }
}