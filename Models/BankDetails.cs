using System.ComponentModel.DataAnnotations;
namespace FoodDeliveryApp.Models
{
    public class BankDetails
    {
        public int BankDetailsId { get; set; }
        public int UserId { get; set; }
        // Navigation to User
        public virtual User User { get; set; }
        [Required]
        public string AccountHolderName { get; set; }
        [Required]
        public string BankName { get; set; }
        [Required]
        public string AccountNumber { get; set; }
        [Required]
        public string IFSCCode { get; set; }
    }
}