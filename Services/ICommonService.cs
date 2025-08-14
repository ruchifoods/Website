using FoodDeliveryApp.Models;

namespace FoodDeliveryApp.Services
{
    public interface ICommonService
    {
        Task<bool> IsEmailDuplicateAsync(string email);
        Task<bool> IsPhoneDuplicateAsync(string phoneNumber);
        Task<bool> IsPhoneNumberAvailableAsync(string phoneNumber, int userId);
        Task<List<Quantity>> GetAllQuantity();
        Task<List<OrderStatusMaster>> GetAllOrderStatus();
        Task<List<FoodType>> GetAllFoodType();
        Task<List<SubQuantity>> GetAllSubQuantity();
    }
}
