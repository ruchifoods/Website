using FoodDeliveryApp.Models;

namespace FoodDeliveryApp.ViewModels
{
    public class Orders
    {
        public List<Order> OrderList { get; set; } = new List<Order>();
        public List<OrderStatusMaster> OrderStatusList { get; set; } = new List<OrderStatusMaster>();
        public List<Order> OldOrderList { get; set; } = new List<Order>();
    }
}