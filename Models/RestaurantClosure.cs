namespace FoodDeliveryApp.Models
{
    public class RestaurantClosure
    {
        public int RestaurantClosureId { get; set; }
        public int RestaurantId { get; set; }
        public virtual Restaurant Restaurant { get; set; }

        public DateTime StartDate { get; set; }  // Start of closure
        public DateTime EndDate { get; set; }    // End of closure

        public string? Reason { get; set; }
    }
}