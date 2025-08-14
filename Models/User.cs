using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryApp.Models
{
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(PhoneNumber), IsUnique = true)]
    public class User
    {
        public int UserId { get; set; }

        // Basic credentials 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsPhoneNumberVerified { get; set; }
        public string Email { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
        public string PasswordHash { get; set; }

        //Role Information
        public int RoleMasterId { get; set; }
        public virtual RoleMaster RoleMaster { get; set; }

        // Audit Information
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<UserAddress>? UserAddresses { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }  // For Customers

        // Optional one-to-one relationships to specialized profiles
        public virtual DeliveryPartnerProfile? DeliveryPartnerProfile { get; set; }
        public virtual RestaurantOwnerProfile? RestaurantOwnerProfile { get; set; }
    }
}