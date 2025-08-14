using FoodDeliveryApp.ViewModels.Customers; 
using FoodDeliveryApp.ViewModels.Common; 
using FoodDeliveryApp.Models;
namespace FoodDeliveryApp.Services
{
    public interface ICustomerService
    {
        Task<bool> RegisterCustomerAsync(CustomerRegisterViewModel model);
        Task<User?> LoginCustomerAsync(LoginViewModel model);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordViewModel model);
        Task<bool> ForgotPasswordAsync(ForgotPasswordViewModel model);
        Task<bool> ResetPasswordAsync(ResetPasswordViewModel model);
        Task<bool> AddAddressAsync(AddressViewModel model, int userId); 
        Task<List<UserAddress>> GetAddressesAsync(int userId);
        Task<UserAddress?> GetAddressByIdAsync(int userId, int userAddressId);
        Task<bool> UpdateAddressAsync(EditAddressViewModel model, int userId);
        Task<bool> DeleteAddressAsync(int userId, int addressId);
        Task<User?> GetCustomerByIdAsync(int userId);
        Task<bool> UpdateCustomerAccountAsync(AccountViewModel model);
        Task<List<MenuCategory>> GetAllMenuCategory();
        Task<List<MenuItem>> GetAllMenuItems(int id);
        Task<MenuItem> GetMenuItemById(int id);
        Task<Order> CreateOrder(List<OrderItem> orderItems, int userId, string pickupTime, int categoryId, string FirstName, string LastName, string PhoneNumber, string EmailAddesss, string comments);
        Task<List<PickUpTime>> GetPickupTimes();
        Task<List<Order>> GetAllCustomerOrders(string firstName, string lastName, string phoneNumber, string emailaddress, int? customerId);
    }
}