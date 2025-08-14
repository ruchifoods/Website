using Microsoft.EntityFrameworkCore;
namespace FoodDeliveryApp.Models
{
    [Index(nameof(RoleName), IsUnique = true, Name = "IX_RoleName_Unique")]
    public class RoleMaster
    {
        public int RoleMasterId { get; set; }
        public string RoleName { get; set; }          // e.g., "Customer", "DeliveryPartner", "RestaurantOwner", "Admin"
        public string? Description { get; set; }      // optional descriptive text
        public bool IsActive { get; set; }

        // Navigation
        public virtual ICollection<User> Users { get; set; }
    }
}