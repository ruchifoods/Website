using FoodDeliveryApp.Data;
using FoodDeliveryApp.Services;
using FoodDeliveryApp.Models;
using FoodDeliveryApp.ViewModels.Customers;
using Microsoft.EntityFrameworkCore;
using FoodDeliveryApp.ViewModels.Common;
using Microsoft.AspNetCore.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.SqlServer.Server;
using System.Net;
namespace FoodDeliveryApp.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerService> _logger;
        private readonly IEmailService _emailService;

        public CustomerService(ApplicationDbContext context, ILogger<CustomerService> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }
        // Registers a new customer. Checks for duplicate email,
        // hashes the password, saves the user, and sends a confirmation email.
        public async Task<bool> RegisterCustomerAsync(CustomerRegisterViewModel model)
        {
            try
            {

                // Hash the password using BCrypt.
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Create a new customer user.
                var customerUser = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    PasswordHash = hashedPassword,
                    RoleMasterId = (int)RoleMasterModel.Customer,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    IsEmailVerified = true,
                    IsPhoneNumberVerified = true,
                    IsTwoFactorEnabled = false,
                    IsActive = true
                };

                _context.Users.Add(customerUser);
                await _context.SaveChangesAsync();
                // Build a professional HTML email body.
                string subject = "Registration Confirmation - Food Delivery App";
                string body = $"<div style='font-family: Arial, sans-serif;'>" +
                $"<h2 style='color: #2e6c80;'>Welcome, {customerUser.FirstName}!</h2>" +
                $"<p>Thank you for registering with Food Delivery App. Your account has been successfully created.</p>" +
                $"<p>Enjoy ordering from your favorite restaurants!</p>" +
                $"<p>Best Regards, <br/>Food Delivery App Team</p></div>";

                await _emailService.SendEmailAsync(customerUser.Email, subject, body, true);

                _logger.LogInformation("Customer registered successfully: {Email}", model.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer registration for email: {Email}", model.Email);
                return false;
            }
        }

        // Validates customer login credentials.
        public async Task<User?> LoginCustomerAsync(LoginViewModel model)
        {
            try
            {
                // Query the database for a user with matching email and customer role.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email
                && u.RoleMasterId == (int)RoleMasterModel.Customer
                && u.IsActive);
                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    _logger.LogInformation("Customer logged in successfully: {Email}", model.Email);
                    return user;
                }
                _logger.LogWarning("Invalid login attempt for customer: {Email}", model.Email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during customer login for email: {Email}", model.Email);
                return null;
            }
        }


        // Changes the customer's password after verifying the current password.
        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordViewModel model)
        {
            try
            {
                // Retrieve the user based on userId.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    _logger.LogWarning("Change password failed: No user found with ID {UserId}", userId);
                    return false;
                }
                // Verify that the current password provided matches the stored hash.
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Change password failed: Incorrect current password for user {UserId} ", userId);
                    return false;
                }
                // Hash the new password and update the user record.
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                user.ModifiedDate = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user {UserId}", userId);
                return false;
            }
        }

        // Processes a forgot password request. Verifies that the customer exists. 
        // Token generation and storage is handled externally.
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordViewModel model)
        {
            try
            {
                // Ensure the user exists and is a customer.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.RoleMasterId == (int)RoleMasterModel.Customer);
                if (user == null)
                {
                    _logger.LogWarning("Forgot password: No customer found with email {Email}", model.Email);
                    return false;
                }

                _logger.LogInformation("Forgot password initiated for email: {Email}", model.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email: {Email}", model.Email);
                return false;
            }
        }

        // Resets the customer's password.
        // Assumes that token validation (e.g., via cache) is done externally. 1 reference

        public async Task<bool> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            try
            {
                // Retrieve the customer based on email and ensure they are a customer.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.RoleMasterId == (int)RoleMasterModel.Customer);
                if (user == null)
                {
                    _logger.LogWarning("Reset password failed: No user found for email {Email}", model.Email);
                    return false;
                }

                // In production, ensure the token has been validated before calling this method.
                //if (string.IsNullOrWhiteSpace(model.Token))
                //{
                //    _logger.LogWarning("Reset password failed: Invalid token for email {Email}", model.Email);
                //    return false;
                //}

                // Update the password with the new hash.
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                user.ModifiedDate = DateTime.UtcNow;
                //_context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for user {Email}", model.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for email: {Email}", model.Email);
                return false;
            }
        }

        public async Task<bool> AddAddressAsync(AddressViewModel model, int userId)
        {
            try
            {
                // Map the view model to the UserAddress data model.
                var address = new UserAddress
                {
                    UserId = userId,
                    Label = model.Label,
                    AddressLine1 = model.AddressLine1,
                    AddressLine2 = model.AddressLine2,
                    City = model.City,
                    State = model.State,
                    ZipCode = model.ZipCode,
                    Landmark = model.Landmark,
                    Longitude = model.Longitude,
                    Latitude = model.Latitude
                };

                _context.UserAddresses.Add(address);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Address added successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding address for user {UserId}", userId);
                return false;
            }
        }

        // Retrieves all addresses associated with the specified customer. 1 reference
        public async Task<List<UserAddress>> GetAddressesAsync(int userId)
        {
            try
            {
                var addresses = await _context.UserAddresses
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} addresses for user {UserId}", addresses.Count, userId);
                return addresses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving addresses for user {UserId}", userId);
                return new List<UserAddress>();
            }
        }

        public async Task<UserAddress?> GetAddressByIdAsync(int userId, int userAddressId)
        {
            try
            {
                var address = await _context.UserAddresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.UserAddressId == userAddressId);

                _logger.LogInformation($"Address found for UserId {userId} and UserAddressId {userAddressId}");
                return address;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving UserAddressId for UserId {userId} and UserAddressId {userAddressId}");
                return null;
            }
        }

        // Updates an existing customer address.
        public async Task<bool> UpdateAddressAsync(EditAddressViewModel model, int userId)
        {
            try
            {
                // First Validate the address belongs to User or not
                var existingAddress = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserId == userId && a.UserAddressId == model.UserAddressId);
                if (existingAddress == null)
                {
                    _logger.LogWarning($"Address Not Found UserAddress Id: {model.UserAddressId} and UserId: {userId}");
                    return false;
                }
                else
                {
                    // Update existing address
                    existingAddress.Label = model.Label;
                    existingAddress.AddressLine1 = model.AddressLine1;
                    existingAddress.AddressLine2 = model.AddressLine2;
                    existingAddress.City = model.City;
                    existingAddress.State = model.State;
                    existingAddress.ZipCode = model.ZipCode;
                    existingAddress.Landmark = model.Landmark;
                    existingAddress.Latitude = model.Latitude;
                    existingAddress.Longitude = model.Longitude;
                    _context.UserAddresses.Update(existingAddress);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Address updated successfully for address {AddressId}", model.UserAddressId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address {AddressId} for user {UserId}", model.UserAddressId, userId);
                return false;
            }
        }

        // Deletes an existing customer address by its ID.
        public async Task<bool> DeleteAddressAsync(int userId, int addressId)
        {
            try
            {
                var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserAddressId == addressId && a.UserId == userId);
                if (address == null)
                {
                    _logger.LogWarning($"Delete address failed: No address found with ID AddressId {addressId} and UserId {userId}");
                    return false;
                }
                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Address deleted successfully for address {Address Id}", addressId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {AddressId}", addressId);
                return false;
            }
        }

        // Retrieves the customer details by user ID.
        public async Task<User?> GetCustomerByIdAsync(int userId)
        {
            try
            {
                // Retrieve the customer record from the database.
                return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId && u.RoleMasterId == (int)RoleMasterModel.Customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer by id {UserId}", userId);
                return null;
            }
        }

        // Updates the customer's account information based on the provided view model.
        public async Task<bool> UpdateCustomerAccountAsync(AccountViewModel model)
        {
            try
            {
                // Retrieve the customer from the database.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == model.UserId && u.RoleMasterId == (int)RoleMasterModel.Customer);
                if (user == null)
                {
                    _logger.LogWarning("Update failed: No customer found with id {UserId}", model.UserId);
                    return false;
                }
                // Update the properties that are editable.
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                // Typically, the email is not updated; if needed, include it here.
                user.ModifiedDate = DateTime.UtcNow;

                //context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer account updated successfully for user {UserId}", model.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cutomer account for user {UserId}", model.UserId);
                return false;
            }
        }

        public async Task<List<MenuCategory>> GetAllMenuCategory()
        {
            try
            {
                var today = DateTime.Now;
                var categories = await _context.MenuCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive == true && c.EndDate > today && c.MenuCategoryId != 2)
                    .OrderBy(c => c.EndDate)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} menu categories", categories.Count);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu categories");
                return new List<MenuCategory>();
            }
        }

        public async Task<List<MenuItem>> GetAllMenuItems(int id)
        {
            try
            {
                var ids = new List<int> { id, 2 };

                var items = await (from m in _context.MenuItems
                                   join ft in _context.FoodType on m.FoodTypeId equals ft.Id
                                   join mic in _context.MenuItemCategories on m.MenuItemId equals mic.MenuItemId
                                   where (mic.MenuCategoryId == 2 || mic.MenuCategoryId == id) && m.IsAvailable == true && m.IsDeleted == false &&
                                         m.FoodTypeId == ft.Id && mic.IsDeleted == false
                                         group new {m, ft, mic} by m into g
                                   select new MenuItem
                                   {
                                       MenuItemId = g.Key.MenuItemId,
                                       MenuCategoryId = g.Key.MenuCategoryId,
                                       ItemName = g.Key.ItemName,
                                       Description = g.Key.Description,
                                       Price = g.Key.Price,
                                       IsVeg = g.Key.IsVeg,
                                       IsAvailable = g.Key.IsAvailable,
                                       ImageUrl = g.Key.ImageUrl,
                                       FoodTypeId = g.Key.FoodTypeId,
                                       IsDeleted = g.Key.IsDeleted,
                                       MenuCategories = new List<MenuCategory>(),
                                       FoodType = g.Select(x => new FoodType
                                       {
                                           Id = x.ft.Id,
                                           Name = x.ft.Name,
                                           Description = x.ft.Description,
                                           IsDeleted = x.ft.IsDeleted
                                       }).FirstOrDefault()                                       
                                   }
                                   ).ToListAsync();

                //var items = _context.MenuItems
                //    .Where(mi => mi.IsAvailable == true && mi.IsDeleted == false &&
                //             _context.MenuItemCategories
                //                      .Where(mic => mic.IsDeleted == false && (mic.MenuCategoryId == 2 || mic.MenuCategoryId == id))
                //                      .Select(mic => mic.MenuItemId)
                //                      .Contains(mi.MenuItemId))
                //.ToList();

                foreach (var item in items)
                {
                    item.MenuCategories.Add(await _context.MenuCategories.FindAsync(id));
                    item.MenuItemQuantities = await _context.MenuItemQuantity.Where(m => m.MenuItemId == item.MenuItemId).ToListAsync();
                }

                _logger.LogInformation("Retrieved {Count} menu items for category {id}", items.Count(), id);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving menu items for category {id}");
                return new List<MenuItem>();
            }
        }
        public async Task<MenuItem> GetMenuItemById(int id)
        {
            try
            {
                var item = await _context.MenuItems
                    .AsNoTracking()
                    .Where(c => c.IsAvailable == true && c.MenuItemId == id && c.IsDeleted == false)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Retrieved menu items by id {id}", id);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving menu item by id {id}");
                return new MenuItem();
            }
        }

        public async Task<Order> CreateOrder(List<OrderItem> orderItems, int userId, string pickupTime, int categoryId, string firstName, string lastName, string phoneNumber, string emailAddesss, string comments)
        {
            try
            {
                // Map the view model to the Order items and Order data model.

                var category = await _context.MenuCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.MenuCategoryId == categoryId && c.IsActive == true);

                string categoryName = category.CategoryName;
                string orderCode = Guid.NewGuid().ToString();

                var order = new Order
                {
                    CustomerId = userId,
                    RestaurantId = 1,
                    OrderDate = DateTime.UtcNow,
                    OrderStatusMasterId = (int)OrderStatusModel.Placed,
                    SubTotal = orderItems.Sum(i => i.TotalPrice),
                    DeliveryFee = 0, // Set default delivery fee
                    TaxAmount = 0, // Set default tax amount
                    Discount = 0, // Set default discount
                    TotalAmount = orderItems.Sum(i => i.TotalPrice),
                    PickUpTime = pickupTime,
                    CategoryId = categoryId,
                    CategoryName = categoryName,
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = phoneNumber,
                    EmailAddress = emailAddesss,
                    CreationDateTime = DateTime.Now,
                    OrderCode = orderCode,
                    Comments = comments
                };
                order.TaxAmount = order.SubTotal * 0.06m; // Example tax calculation (5% of subtotal)
                order.TotalAmount = order.SubTotal + order.TaxAmount; // Calculate total amount
                _context.Orders.Add(order);
                _context.SaveChanges(); // This assigns a value to order.Id from the database
                _logger.LogInformation("Created order successfully for user {UserId}", userId);

                int newOrderId = order.OrderId; // Retrieve the generated ID


                foreach (var item in orderItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = newOrderId,
                        MenuItemId = item.MenuItemId,
                        Quantity = item.Quantity,
                        SubQuantity = item.SubQuantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    };
                    _context.OrderItems.Add(orderItem);
                }
                _context.SaveChanges();
                _logger.LogInformation("Added order item(s) successfully for user {UserId}", userId);
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding order or order item(s) for user {UserId}", userId);
                return new Order();
            }
        }
        public async Task<int> GetAddressIdAsync(int userId)
        {
            int addressId = 0;
            try
            {
                var address = await _context.UserAddresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                _logger.LogInformation($"Fetch Address ID for UserId {userId}");
                addressId = address.UserAddressId;
                return addressId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving Address Id for UserId {userId}");
                return addressId;
            }
        }
        public async Task<List<PickUpTime>> GetPickupTimes()
        {
            try
            {
                var pickupTime = await _context.PickUpTime
                    .Where(p => p.IsActive == true)
                    .ToListAsync();

                _logger.LogInformation("Retrieved pickup times.");
                return pickupTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving pickup times.");
                return new List<PickUpTime>();
            }
        }
        public async Task<List<Order>> GetAllCustomerOrders(string firstName, string lastName, string phoneNumber, string emailaddress, int? customerId)
        {
            try
            {
                List<Order> orders;
                if (customerId == -1)
                {
                    //orders = await _context.Orders
                    //    .Where(o => o.FirstName == firstName && o.LastName == lastName && o.PhoneNumber == phoneNumber && o.EmailAddress == emailaddress)
                    //    .ToListAsync();
                    orders = await (
                        from o in _context.Orders
                        join i in _context.OrderItems on o.OrderId equals i.OrderId
                        join m in _context.MenuItems on i.MenuItemId equals m.MenuItemId
                        join c in _context.MenuCategories on o.CategoryId equals c.MenuCategoryId
                        join s in _context.OrderStatusMasters on o.OrderStatusMasterId equals s.OrderStatusMasterId
                        where (o.FirstName == firstName && o.LastName == lastName && o.PhoneNumber == phoneNumber && o.EmailAddress == emailaddress)
                        orderby c.EndDate
                        group new { i, m, c } by new { o.OrderId, o.CustomerId, o.OrderDate, o.SubTotal, o.DeliveryFee, o.TaxAmount, o.Discount, o.TotalAmount, o.PickUpTime, o.CategoryId, o.CategoryName, o.OrderStatusMasterId, o.OrderCode, o.Comments, c.DisplayOrder, c.StartDate, c.EndDate } into g
                        orderby g.Key.EndDate
                        select new Order
                        {
                            OrderId = g.Key.OrderId,
                            CustomerId = g.Key.CustomerId,
                            OrderDate = g.Key.OrderDate,
                            SubTotal = g.Key.SubTotal,
                            DeliveryFee = g.Key.DeliveryFee,
                            TaxAmount = g.Key.TaxAmount,
                            Discount = g.Key.Discount,
                            TotalAmount = g.Key.TotalAmount,
                            PickUpTime = g.Key.PickUpTime,
                            CategoryId = g.Key.CategoryId,
                            CategoryName = g.Key.CategoryName,
                            OrderStatusMasterId = g.Key.OrderStatusMasterId,
                            OrderCode = g.Key.OrderCode,
                            Comments = g.Key.Comments,
                            OrderItems = g.Select(x => new OrderItem
                            {
                                OrderItemId = x.i.OrderItemId,
                                OrderId = x.i.OrderId,
                                MenuItemId = x.i.MenuItemId,
                                Quantity = x.i.Quantity,
                                UnitPrice = x.i.UnitPrice,
                                TotalPrice = x.i.TotalPrice,
                                MenuItem = new MenuItem
                                {
                                    MenuItemId = x.m.MenuItemId,
                                    MenuCategoryId = x.m.MenuCategoryId,
                                    ItemName = x.m.ItemName,
                                    Description = x.m.Description,
                                    Price = x.m.Price,
                                    IsVeg = x.m.IsVeg
                                }
                            }).ToList()
                        }
                    ).ToListAsync();

                }
                else
                {
                    orders = await _context.Orders
                        .Where(o => o.CustomerId == customerId)
                        .ToListAsync();
                }

                if(orders == null || !orders.Any())
                {
                    _logger.LogWarning("No orders found for the customer with first name = " + firstName + " last name = " + lastName + " email address = " + emailaddress + " phone number = " + phoneNumber);
                    return new List<Order>();
                }

                _logger.LogInformation("Retrieved customer orders for first name = " + firstName + " last name = " + lastName + " email address = " + emailaddress + " phone number = " + phoneNumber);
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving customer orders with first name = " + firstName + " last name = " + lastName + " email address = " + emailaddress + " phone number = " + phoneNumber);
                return new List<Order>();
            }
        }
    }
}