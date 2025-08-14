using System.ComponentModel.DataAnnotations;

namespace FoodDeliveryApp.ViewModels.RestaurantOwners
{
    public class RestaurantOwnerRegisterViewModel
    {
        [Display(Name = "First Name")]
        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Display(Name = "Phone Number")]
        [Required(ErrorMessage = "Phone Number is required")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
        public string Password { get; set; }

        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }


        // Business-specific fields for verification [Display (Name = "Business License Number")]
        [Required(ErrorMessage = "Business License Number is required")]
        public string BusinessLicenseNumber { get; set; }

        [Display(Name = "GSTIN")]
        public string GSTIN { get; set; }

        [Display(Name = "Business Registration Number")]
        [Required(ErrorMessage = "Business Registration Number is required")]
        public string BusinessRegistrationNumber { get; set; }
        [Display(Name = "Account Holder Name")]
        [Required(ErrorMessage = "Account Holder Name is required")]
        public string AccountHolderName { get; set; }

        [Display(Name = "Bank Name")]
        [Required(ErrorMessage = "Bank Name is required")]
        public string BankName { get; set; }

        [Display(Name = "Account Number")]
        [Required(ErrorMessage = "Account Number is required")]
        public string AccountNumber { get; set; }

        [Display(Name = "IFSC Code")]
        [Required(ErrorMessage = "IFSC Code is required")]
        public string IFSCCode { get; set; }
    }
}