using FoodDeliveryApp.Models;
using FoodDeliveryApp.ViewModels.Common;
using FoodDeliveryApp.ViewModels.RestaurantOwners;
namespace FoodDeliveryApp.Services
{
    public interface IRestaurantOwnerService
    {
        // Registration and login operation
        Task<bool> RegisterRestaurantOwnerAsync(RestaurantOwnerRegisterViewModel model);
        Task<User?> LoginRestaurantOwnerAsync(LoginViewModel model);

        // Password management
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordViewModel model);
        Task<bool> ForgotPasswordAsync(ForgotPasswordViewModel model);
        Task<bool> ResetPasswordAsync(ResetPasswordViewModel model);

        // Account update operations
        Task<bool> UpdateRestaurantOwnerAccountAsync(AccountViewModel model);
        Task<User?> GetRestaurantOwnerByIdAsync(int userId);
        Task<bool> IsAccountVerifiedAsync(int userId);
        Task<RestaurantOwnerProfile?> GetRestaurantOwnerProfileAsync(int userId);

        // Address management
        Task<EditAddressViewModel?> GetRestaurantOwnerAddressAsync(int userId);
        Task<bool> AddOrUpdateAddressAsync(int userId, EditAddressViewModel model);

        // Bank details operations
        Task<BankDetailsViewModel?> GetBankDetailsByUserIdAsync(int userId);
        Task<bool> UpdateBankDetailsAsync(BankDetailsViewModel model);

        // Business details operations O references
        Task<RestaurantOwnerBusinessDetailsViewModel?> GetBusinessDetailsByUserIdAsync(int userId);
        Task<bool> AddOrUpdateBusinessDetailsAsync(RestaurantOwnerBusinessDetailsViewModel model);
        Task<List<Order?>> GetAllOrders(int userId);
        Task<bool> UpdateOrders(int orderId, int orderStatus);
        Task<List<MenuItem>> GetAllMenuItemsStored();
        Task<bool> AddEditMenuItemAsync(MenuItem model, int? menuItemID, List<int> selectedQuantityIds, List<int> selectedCategoryIds, int selectedFoodTypeId);
        Task<List<MenuCategory>> GetAllMenuCategoryStored();
        Task<MenuItem> GetMenuItemById(int id);
        Task<List<MenuItemQuantity>> GetQuantityById(int menuItemId);
        Task<List<MenuCategory>> GetCategoriesByMenuId(int menuItemId);
        Task<List<MenuCategory>> GetAllMenuCategoriesStored();
        Task<MenuCategory> GetMenuCategoryById(int id);
        Task<bool> AddEditCategoryAsync(MenuCategory model, int? categoryID, List<int> selectedCategoryIds);
        //Task<List<OrderStatusMaster>> GetAllOrderStatus();
        Task<bool> DeleteCategoryById(int id);
        Task<bool> DeleteMenuItemById(int id);
        Task<List<PickUpTime>> GetAllPickUpTime();
        Task<PickUpTime> GetPickupTimeById(int id);
        Task<bool> AddEditPickupTimeAsync(PickUpTime model, int? id);
        Task<bool> DeletePickupTimeById(int id);
        Task<List<Order?>> GetAllOldOrders(int userId);
    }
}