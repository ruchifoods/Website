using Elfie.Serialization;
using FoodDeliveryApp.Data;
using FoodDeliveryApp.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
namespace FoodDeliveryApp.Services
{
    public class CommonService : ICommonService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommonService> _logger;
        public CommonService(ApplicationDbContext context, ILogger<CommonService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<bool> IsEmailDuplicateAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
        public async Task<bool> IsPhoneDuplicateAsync(string phoneNumber)
        {
            return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
        }
        public async Task<bool> IsPhoneNumberAvailableAsync(string phoneNumber, int userId)
        {
            return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber && u.UserId != userId);
        }
        public async Task<List<Quantity>> GetAllQuantity()
        {
            try
            {
                var quantities = await _context.Quantity
                    .AsNoTracking()
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} quantities", quantities.Count);
                return quantities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu categories");
                return new List<Quantity>();
            }
        }
        public async Task<List<SubQuantity>> GetAllSubQuantity()
        {
            try
            {
                var subQuantities = await _context.SubQuantity
                    .AsNoTracking()
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} subQuantities", subQuantities.Count);
                return subQuantities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subQuantities");
                return new List<SubQuantity>();
            }
        }
        public async Task<List<OrderStatusMaster>> GetAllOrderStatus()
        {
            try
            {
                var orderStatusList = await _context.OrderStatusMasters
                    .AsNoTracking()
                    .Where(c => c.IsActive == true)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} OrderStatusMasters", orderStatusList.Count);
                return orderStatusList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu categories");
                return new List<OrderStatusMaster>();
            }
        }
        public async Task<List<FoodType>> GetAllFoodType()
        {
            try
            {
                var foodTypes = await _context.FoodType
                    .Where(ft => ft.IsDeleted == false)
                    .AsNoTracking()
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} foodTypes", foodTypes.Count);
                return foodTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu categories");
                return new List<FoodType>();
            }
        }

    }
}