using System.ComponentModel.DataAnnotations;

namespace FoodDeliveryApp.Models
{
    public class FoodType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}