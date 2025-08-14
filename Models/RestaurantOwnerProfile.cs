namespace FoodDeliveryApp.Models
{
    public class RestaurantOwnerProfile
    {
        public int RestaurantOwnerProfileId { get; set; }

        // 1:1 relationship to User
        public int UserId { get; set; }
        public virtual User User { get; set; }

        // Role-specific fields for a restaurant owner
        public string BusinessLicenseNumber { get; set; }
        public string GSTIN { get; set; }    // if relevant in your region
        public string BusinessRegistrationNumber { get; set; }
        public bool IsVerified { get; set; } // whether Admin verified the owner’s documents
        public string? AdminRemarks { get; set; } //When Admin Approvde or Reject the Remarks will be stored here

        // Store bank details, payout info etc...
        //public string AccountHolderName { get; set; }
        //public string BankName { get; set; }  
        //public string AccountNumber { get; set; }
        //public string IFSCCode { get; set; }
    }
}