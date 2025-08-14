using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FoodDeliveryApp.Models
{
    public class OrderItemList
    {
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public List<PickUpTime> PickUpTimes { get; set; } = new List<PickUpTime>();
        public int CategoryId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^(\+1\s?)?(\()?(\d{3})(\))?[-.\s]?(\d{3})[-.\s]?(\d{4})$",
        ErrorMessage = "Enter a valid US phone number")]
        //[RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits")]
        public string PhoneNumber { get; set; }
        public string EmailAddesss { get; set; }
        public string? Comments { get; set; }
    }
}