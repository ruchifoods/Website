using System.ComponentModel.DataAnnotations;
namespace FoodDeliveryApp.ViewModels.Common
{
    public class BankDetailsViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Account Holder Name is required")]
        public string AccountHolderName { get; set; }

        [Required(ErrorMessage = "Bank Name is required")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "Account Number is required")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "IFSC Code is required")]
        public string IFSCCode { get; set; }
    }
}
