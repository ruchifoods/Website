using FoodDeliveryApp.Data;
using FoodDeliveryApp.Services;
using FoodDeliveryApp.ViewModels.RestaurantOwners;
using Microsoft.EntityFrameworkCore;
using FoodDeliveryApp.Models;
using FoodDeliveryApp.ViewModels.Common;
using System.Net;
using Microsoft.SqlServer.Server;
using Mono.TextTemplating;
using System.Reflection.Emit;
using System.ComponentModel;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
namespace FoodDeliveryApp.Services
{
    public class RestaurantOwnerService : IRestaurantOwnerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RestaurantOwnerService> _logger;
        private readonly IEmailService _emailService;
        public RestaurantOwnerService(ApplicationDbContext context, ILogger<RestaurantOwnerService> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<bool> RegisterRestaurantOwnerAsync(RestaurantOwnerRegisterViewModel model)
        {
            try
            {
                //// Preliminary checks (outside transaction)
                //if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                //{
                //    _logger.LogWarning("Registration failed: Email already exists ({Email})", model.Email);
                //    return false;
                //}
                //if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))                    
                //{
                //    _logger.LogWarning("Registration failed: Phone Number already exists ({Phone Number})", model.PhoneNumber);
                //    return false;
                //}

                // Hash the password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                // Begin transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1) Create the user
                    var ownerUser = new User
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                        Email = model.Email,
                        PasswordHash = hashedPassword,
                        RoleMasterId = (int)RoleMasterModel.RestaurantOwner,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        IsActive = true,
                        IsEmailVerified = false,
                        IsPhoneNumberVerified = false,
                        IsTwoFactorEnabled = false
                    };
                    _context.Users.Add(ownerUser);
                    await _context.SaveChangesAsync();

                    // 2) Create the RestaurantOwnerProfile
                    var ownerProfile = new RestaurantOwnerProfile
                    {
                        UserId = ownerUser.UserId,
                        BusinessLicenseNumber = model.BusinessLicenseNumber,
                        GSTIN = model.GSTIN,
                        BusinessRegistrationNumber = model.BusinessRegistrationNumber,
                        IsVerified = false
                    };
                    _context.RestaurantOwnerProfiles.Add(ownerProfile);

                    // 3) Create the BankDetails record
                    var ownerBankDetails = new BankDetails
                    {
                        UserId = ownerUser.UserId,
                        AccountHolderName = model.AccountHolderName,
                        AccountNumber = model.AccountNumber,
                        BankName = model.BankName,
                        IFSCCode = model.IFSCCode
                    };
                    _context.BankDetails.Add(ownerBankDetails);

                    await _context.SaveChangesAsync();

                    // 4) Commit the transaction
                    await transaction.CommitAsync();
                }
                catch (Exception dBEx)
                {
                    _logger.LogError(dBEx, "Error saving user/profile/bank details within transaction for email: {Email}", model.Email);

                    // Explicitly roll back if something went wrong
                    await transaction.RollbackAsync();

                    // We don't send the email if transaction fails
                    return false;
                }

                // Only send email *after* the transaction is committed
                string subject = "Restrautrant Owner Registration - Verification Pending";

                string body = @"<!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <style>
                            body { font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }
                            .email-container { max-width: 600px; margin: 30px auto; background: #ffffff; padding: 20px; border: 1px solid; }
                            .header { background: #004085; padding: 20px; color: #ffffff; text-align: center; }
                            .content { margin: 20px 0; line-height: 1.6; }
                            .footer { text-align: center; font-size: 12px; color: #777777; margin-top: 20px; }
                            a { color: #004085; text-decoration: none; }
                        </style>
                        </head>
                    <body>
                        <div class='email-container'>
                        <div class='header'>
                        <h2>Ruchi Kitchen</h2>
                        </div>
                        <div class='content'>
                            <p>Dear " + model.FirstName + @",</p>
                            <p>Thank you for registering as a Restaurant Owner on Ruchi Kitchen. Your account is currently under process.</p>
                            <p>You will receive a notification email once the verification process is complete.</p>
                            <p>If you have any questions, feel free to <a href='mailto:'>contact our support</p>
                        </div>
                            <div class='footer'>
                                <p>&copy; " + DateTime.Now.Year + @" Ruchi Kitchen. All rights reserved.</p>
                            </div>
                        </div>
                    </body>";

                await _emailService.SendEmailAsync(model.Email, subject, body, true);

                _logger.LogInformation("Restrautrant Owner registered successfully: {Email}", model.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during restaurant owner registration for email: {Email}", model.Email);
                return false;
            }
        }


        public async Task<User?> LoginRestaurantOwnerAsync(LoginViewModel model)
        {
            try
            {
                var user = await _context.Users
                .AsNoTracking()
                //.Include(u => u.RestaurantOwnerProfile)
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.RoleMasterId == (int)RoleMasterModel.RestaurantOwner);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    _logger.LogInformation("Restaurant Owner logged in successfully: {Email}", model.Email);
                    return user;
                }

                _logger.LogWarning("Invalid login attempt for restaurant owner: {Email}", model.Email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during restaurant owner login for email: {Email}", model.Email);
                return null;

            }
        }                    


        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordViewModel model)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    _logger.LogWarning("Change password failed: No user found with ID {UserId}", userId);
                    return false;
                }

                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Change password failed: Incorrect current password for user {UserId}", userId);
                    return false;
                }

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

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordViewModel model)
        {
            try
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == model.Email && u.RoleMasterId == (int)RoleMasterModel.RestaurantOwner);
                if (user == null)
                {
                    _logger.LogWarning("Forgot password: No restaurant owner found with email {Email}", model.Email);
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


        public async Task<bool> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.RoleMasterId == (int)RoleMasterModel.RestaurantOwner);
                if (user == null)
                {
                    _logger.LogWarning("Reset password failed: No user found for email {Email}", model.Email);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(model.Token))
                {
                    _logger.LogWarning("Reset password failed: Invalid token for email {Email}", model.Email);
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                user.ModifiedDate = DateTime.UtcNow;
                _context.Users.Update(user);
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


        public async Task<bool> AddOrUpdateAddressAsync(int userId, EditAddressViewModel model)
        {
            try
            {
                // Check if an address already exists for the user.
                var existingAddress = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserId == userId);
                if (existingAddress != null)
                {
                    // Update existing address
                    existingAddress.AddressLine1 = model.AddressLine1;
                    existingAddress.AddressLine2 = model.AddressLine2;
                    existingAddress.City = model.City;
                    existingAddress.State = model.State;
                    existingAddress.ZipCode = model.ZipCode;
                    existingAddress.Landmark = model.Landmark;
                    //existingAddress.Latitude = model.Latitude;
                    //existingAddress.Longitude = model.Longitude;
                    //_context.UserAddresses.Update(existingAddress);
                }
                else
                {
                    // Add new address
                    var address = new UserAddress
                    {
                        UserId = userId,
                        AddressLine1 = model.AddressLine1,
                        AddressLine2 = model.AddressLine2,
                        City = model.City,
                        State = model.State,
                        ZipCode = model.ZipCode,
                        Landmark = model.Landmark
                        //,
                        //Latitude = model.Latitude,
                        //Longitude = model.Longitude
                    };
                    _context.UserAddresses.Add(address);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Address saved successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving address for user {UserId}", userId);
                return false;
            }
        }

        public async Task<User?> GetRestaurantOwnerByIdAsync(int userId)
        {
            try
            {
                return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId && u.RoleMasterId == (int)RoleMasterModel.RestaurantOwner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving restaurant owner by id {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateRestaurantOwnerAccountAsync(AccountViewModel model)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == model.UserId && u.RoleMasterId == (int)RoleMasterModel.RestaurantOwner);
                if (user == null)
                {
                    _logger.LogWarning("Update failed: No restaurant owner found with id {UserId}", model.UserId);
                    return false;
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.ModifiedDate = DateTime.UtcNow;
                //_context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Restaurant owner account updated successfully for user {UserId}", model.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account for user {UserId}", model.UserId);
                return false;
            }
        }

        public async Task<bool> IsAccountVerifiedAsync(int userId)
        {
            try
            {
                var profile = await _context.RestaurantOwnerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                return profile != null && profile.IsVerified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking account verification for user {UserId}", userId);
                return false;
            }
        }


        public async Task<RestaurantOwnerProfile?> GetRestaurantOwnerProfileAsync(int userId)
        {
            try
            {

                return await _context.RestaurantOwnerProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving owner profile for user {UserId}", userId);
                return null;
            }
        }


        public async Task<EditAddressViewModel?> GetRestaurantOwnerAddressAsync(int userId)
        {
            try
            {
                var address = await _context.UserAddresses.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);
                if (address != null)
                {
                    EditAddressViewModel editAddressViewModel = new EditAddressViewModel()
                    {
                        UserAddressId = address.UserAddressId,
                        AddressLine1 = address.AddressLine1,
                        AddressLine2 = address.AddressLine2,
                        City = address.City,
                        State = address.State,
                        ZipCode = address.ZipCode,
                        Landmark = address.Landmark,
                        Latitude = address.Latitude,
                        Longitude = address.Longitude
                    };
                    return editAddressViewModel;
                }
                return new EditAddressViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving address with Id {UserId}", userId);
                return new EditAddressViewModel();
            }
        }


        public async Task<RestaurantOwnerBusinessDetailsViewModel?> GetBusinessDetailsByUserIdAsync(int userId)
        {
            try
            {
                var businessDetails = await _context.RestaurantOwnerProfiles.AsNoTracking().FirstOrDefaultAsync(b => b.UserId == userId);

                if (businessDetails != null)
                {
                    RestaurantOwnerBusinessDetailsViewModel restaurantOwnerBusinessDetailsViewModel = new RestaurantOwnerBusinessDetailsViewModel
                    {
                        RestaurantOwnerProfileId = businessDetails.RestaurantOwnerProfileId,
                        BusinessLicenseNumber = businessDetails.BusinessLicenseNumber,
                        GSTIN = businessDetails.GSTIN,
                        BusinessRegistrationNumber = businessDetails.BusinessRegistrationNumber
                    };

                    return restaurantOwnerBusinessDetailsViewModel;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Business details for user {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> AddOrUpdateBusinessDetailsAsync(RestaurantOwnerBusinessDetailsViewModel model)
        {
            try
            {
                // Check if business Details already exists for the user.
                var existingBusinessDetails = await _context.RestaurantOwnerProfiles.FirstOrDefaultAsync(a => a.UserId == model.UserId);
                if (existingBusinessDetails != null)
                {
                    // Update existing business Details
                    existingBusinessDetails.BusinessLicenseNumber = model.BusinessLicenseNumber;
                    existingBusinessDetails.BusinessRegistrationNumber = model.BusinessRegistrationNumber;
                    existingBusinessDetails.GSTIN = model.GSTIN;
                }
                else
                {
                    // Add new Business Details
                    var restaurantOwnerProfile = new RestaurantOwnerProfile
                    {
                        UserId = model.UserId,
                        BusinessLicenseNumber = model.BusinessLicenseNumber,
                        BusinessRegistrationNumber = model.BusinessRegistrationNumber,
                        GSTIN = model.GSTIN
                    };

                    _context.RestaurantOwnerProfiles.Add(restaurantOwnerProfile);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Business Details saved successfully for user {UserId}", model.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Business details for user {UserId}", model.UserId);
                return false;
            }
        }

        public async Task<BankDetailsViewModel?> GetBankDetailsByUserIdAsync(int userId)
        {
            try
            {
                var bankDetails = await _context.BankDetails.AsNoTracking().FirstOrDefaultAsync(b => b.UserId == userId);
                if (bankDetails != null)
                {
                    BankDetailsViewModel bankDetailsViewModel = new BankDetailsViewModel();
                    bankDetailsViewModel.AccountHolderName = bankDetails.AccountHolderName;
                    bankDetailsViewModel.BankName = bankDetails.BankName;
                    bankDetailsViewModel.AccountNumber = bankDetails.AccountNumber;
                    bankDetailsViewModel.IFSCCode = bankDetails.IFSCCode;
                    bankDetails.UserId = userId;

                    return bankDetailsViewModel;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bank details for user {UserId}", userId);
                return null;
            }
        }
        public async Task<bool> UpdateBankDetailsAsync(BankDetailsViewModel model)
        {
            try
            {
                var bankDetails = await _context.BankDetails.FirstOrDefaultAsync(b => b.UserId == model.UserId);
                if (bankDetails != null)
                {
                    bankDetails.AccountHolderName = model.AccountHolderName;
                    bankDetails.BankName = model.BankName;
                    bankDetails.AccountNumber = model.AccountNumber;
                    bankDetails.IFSCCode = model.IFSCCode;
                    //_context.BankDetails.Update(bankDetails);
                }
                else
                {
                    bankDetails = new BankDetails
                    {
                        UserId = model.UserId,
                        AccountHolderName = model.AccountHolderName,
                        BankName = model.BankName,
                        AccountNumber = model.AccountNumber,
                        IFSCCode = model.IFSCCode
                    };
                    _context.BankDetails.Add(bankDetails);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Bank Details updated successfully for user {UserId}", model.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank details for user {UserId}", model.UserId);
                return false;
            }
        }
        public async Task<List<Order?>> GetAllOrders(int userId)
        {
            try
            {
                var yesterdayTime = DateTime.Now.AddDays(-1);
                var ordersWithItems = await (
                    from o in _context.Orders
                    join i in _context.OrderItems on o.OrderId equals i.OrderId
                    join m in _context.MenuItems on i.MenuItemId equals m.MenuItemId
                    join c in _context.MenuCategories on o.CategoryId equals c.MenuCategoryId
                    join s in _context.OrderStatusMasters on o.OrderStatusMasterId equals s.OrderStatusMasterId
                    //where (o.OrderStatusMasterId == (int)OrderStatusModel.Placed)
                    where ((o.OrderStatusMasterId != (int)OrderStatusModel.Delivered) && (o.OrderStatusMasterId != (int)OrderStatusModel.Cancelled))
                    //&& ((o.EndDateTime == null) || (o.EndDateTime > yesterdayTime))
                    orderby c.EndDate
                    group new { i, m, c } by new { o.OrderId, o.CustomerId, o.OrderDate, o.SubTotal, o.DeliveryFee, o.TaxAmount, o.Discount, o.TotalAmount, o.PickUpTime, o.CategoryId, o.CategoryName, o.OrderStatusMasterId, o.OrderCode, o.Comments, o.FirstName, o.LastName, o.PhoneNumber, o.EmailAddress, o.Notes, c.DisplayOrder, c.StartDate, c.EndDate } into g orderby g.Key.EndDate
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
                        FirstName = g.Key.FirstName,
                        LastName = g.Key.LastName,
                        PhoneNumber = g.Key.PhoneNumber,
                        EmailAddress = g.Key.EmailAddress,
                        Notes = g.Key.Notes,
                        OrderItems = g.Select(x => new OrderItem
                        {
                            OrderItemId = x.i.OrderItemId,
                            OrderId = x.i.OrderId,
                            MenuItemId = x.i.MenuItemId,
                            Quantity = x.i.Quantity,
                            SubQuantity = x.i.SubQuantity,
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


                if (ordersWithItems == null)
                {
                    _logger.LogInformation("Not retrieved orders.");
                    return new List<Order?>();
                }
                else
                {
                    _logger.LogInformation("Retrieved {Count} orders for user {UserId}", ordersWithItems.Count, userId);
                    return ordersWithItems;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details for UserID {UserId}", userId);
                return new List<Order?>();
            }
        }
        public async Task<List<Order?>> GetAllOldOrders(int userId)
        {
            try
            {
                var currentTime = DateTime.Now;
                var ordersWithItems = await (
                    from o in _context.Orders
                    join i in _context.OrderItems on o.OrderId equals i.OrderId
                    join m in _context.MenuItems on i.MenuItemId equals m.MenuItemId
                    join c in _context.MenuCategories on o.CategoryId equals c.MenuCategoryId
                    join s in _context.OrderStatusMasters on o.OrderStatusMasterId equals s.OrderStatusMasterId
                    where ((o.OrderStatusMasterId == (int)OrderStatusModel.Delivered) || (o.OrderStatusMasterId == (int)OrderStatusModel.Cancelled))
                    && (o.EndDateTime < currentTime)
                    
                    orderby o.EndDateTime descending, o.OrderStatusMasterId descending
                    group new { i, m, c } by new { o.OrderId, o.CustomerId, o.OrderDate, o.SubTotal, o.DeliveryFee, o.TaxAmount, o.Discount, o.TotalAmount, o.PickUpTime, o.CategoryId, o.CategoryName, o.OrderStatusMasterId, o.OrderCode, o.Comments, o.FirstName, o.LastName, o.PhoneNumber, o.EmailAddress, o.Notes, c.DisplayOrder, c.StartDate, c.EndDate } into g
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
                        FirstName = g.Key.FirstName,
                        LastName = g.Key.LastName,
                        PhoneNumber = g.Key.PhoneNumber,
                        EmailAddress = g.Key.EmailAddress,
                        Notes = g.Key.Notes,
                        OrderItems = g.Select(x => new OrderItem
                        {
                            OrderItemId = x.i.OrderItemId,
                            OrderId = x.i.OrderId,
                            MenuItemId = x.i.MenuItemId,
                            Quantity = x.i.Quantity,
                            SubQuantity = x.i.SubQuantity,
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


                if (ordersWithItems == null)
                {
                    _logger.LogInformation("Not retrieved orders.");
                    return new List<Order?>();
                }
                else
                {
                    _logger.LogInformation("Retrieved {Count} orders for user {UserId}", ordersWithItems.Count, userId);
                    return ordersWithItems;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details for UserID {UserId}", userId);
                return new List<Order?>();
            }
        }
        public async Task<bool> UpdateOrders(int orderId, int orderStatus)
        {
            try
            {
                //var order = await _context.Orders
                //    .AsNoTracking()
                //    .FirstOrDefaultAsync(o => o.OrderId == orderId);
                var order = await _context.Orders
                    .Where(o => o.OrderId == orderId)
                    .Include(o => o.OrderStatusMaster)
                    .FirstOrDefaultAsync();
                var currentTime = DateTime.Now;
                if (order != null)
                {
                    //order.OrderStatusMasterId = 5;
                    order.OrderStatusMasterId = orderStatus;
                    order.EndDateTime = currentTime;
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Update order delivered for OrderId {orderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for OrderId {orderId}", orderId);
                return false;
            }
        }
        public async Task<List<MenuItem>> GetAllMenuItemsStored()
        {
            try
            {
                var items = await (
                from m in _context.MenuItems
                join mic in _context.MenuItemCategories on m.MenuItemId equals mic.MenuItemId
                join c in _context.MenuCategories on mic.MenuCategoryId equals c.MenuCategoryId
                //join mf in _context.MenuItemFoodType on m.MenuItemId equals mf.MenuItemId
                join f in _context.FoodType on m.FoodTypeId equals f.Id
                where !m.IsDeleted
                group new { m, c, f } by m into g orderby g.Key.FoodTypeId descending
                select new MenuItem
                {
                    MenuItemId = g.Key.MenuItemId,
                    ItemName = g.Key.ItemName,
                    Description = g.Key.Description,
                    Price = g.Key.Price,
                    IsVeg = g.Key.IsVeg,
                    IsAvailable = g.Key.IsAvailable,
                    ImageUrl = g.Key.ImageUrl,
                    MenuCategories = g
                        .GroupBy(x => x.c.MenuCategoryId)
                        .Select(grp => new MenuCategory
                        {
                            MenuCategoryId = grp.Key,
                            CategoryName = grp.First().c.CategoryName,
                            Description = grp.First().c.Description,
                            DisplayOrder = grp.First().c.DisplayOrder,
                            IsActive = grp.First().c.IsActive,
                            StartDate = grp.First().c.StartDate,
                            EndDate = grp.First().c.EndDate
                        }).ToList(),
                    FoodType = g
                        .GroupBy(x => x.f.Id)
                        .Select(grp => new FoodType
                        {
                            Id = grp.First().f.Id,
                            Name = grp.First().f.Name,
                            Description = grp.First().f.Description
                        }).FirstOrDefault(),
                }).ToListAsync();



                if (items == null)
                {
                    _logger.LogInformation("Not retrieved menus.");
                    return new List<MenuItem>();
                }
                else
                {
                    _logger.LogInformation("Retrieved {Count} menus.", items.Count);
                    return items;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving menus");
                return new List<MenuItem>();
            }
        }
        public async Task<List<MenuCategory>> GetAllMenuCategoriesStored()
        {
            try
            {
                InActiveMenuCategories();
                var today = DateTime.Now;
                var items = await _context.MenuCategories
                    //.Where(c => c.IsDeleted == false)
                    .Where(c => c.IsActive == true && c.EndDate > today && c.MenuCategoryId != 2)
                    .OrderBy(c => c.EndDate)
                    .ToListAsync();

                if (items == null)
                {
                    _logger.LogInformation("Not retrieved category.");
                    return new List<MenuCategory>();
                }
                else
                {
                    _logger.LogInformation("Retrieved {Count} category.", items.Count);
                    return items;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving category");
                return new List<MenuCategory>();
            }
        }
        public async Task<List<MenuCategory>> GetAllMenuCategoryStored()
        {
            try
            {
                InActiveMenuCategories();
                var today = DateTime.Now;
                var categories = await _context.MenuCategories
                    .AsNoTracking()
                    .Where(c => c.IsActive == true && c.EndDate > today)
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

        public async Task<List<PickUpTime>> GetAllPickUpTime()
        {
            try
            {
                var pickupTime = await _context.PickUpTime
                    .AsNoTracking()
                    .Where(c => c.IsActive == true)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} pickup time", pickupTime.Count);
                return pickupTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pickup time");
                return new List<PickUpTime>();
            }
        }
        public async Task<bool> AddEditMenuItemAsync(MenuItem model, int? menuItemID, List<int> selectedQuantityIds, List<int> selectedCategoryIds, int selectedFoodTypeId)
        {
            try
            {
                if (menuItemID == 0)
                {
                    var menuItem = new MenuItem
                    {
                        ItemName = model.ItemName,
                        Description = model.Description,
                        Price = model.Price,
                        IsVeg = model.IsVeg,
                        IsAvailable = model.IsAvailable,
                        ImageUrl = model.ImageUrl,
                        MenuCategoryId = model.MenuCategoryId,
                        FoodTypeId = selectedFoodTypeId
                    };
                    _context.MenuItems.Add(menuItem);
                    _context.SaveChanges();
                    int generatedId = menuItem.MenuItemId;

                    AddQuantityWithMenuItem(generatedId, selectedQuantityIds);
                    AddCategoryWithMenuItem(generatedId, selectedCategoryIds);
                    AddFoodTypeWithMenuItem(generatedId, selectedFoodTypeId);

                    _logger.LogInformation("Menu item added successfully: {ItemName}", model.ItemName);
                    return true;
                }
                else
                {
                    var menuItem = new MenuItem
                    {
                        MenuItemId = model.MenuItemId, // If updating, otherwise set to 0 for new item
                        ItemName = model.ItemName,
                        Description = model.Description,
                        Price = model.Price,
                        IsVeg = model.IsVeg,
                        IsAvailable = model.IsAvailable,
                        ImageUrl = model.ImageUrl,
                        MenuCategoryId = model.MenuCategoryId,
                        FoodTypeId = selectedFoodTypeId
                    };
                    _context.MenuItems.Update(menuItem);
                    _context.SaveChanges();

                    AddQuantityWithMenuItem(model.MenuItemId, selectedQuantityIds);
                    AddCategoryWithMenuItem(model.MenuItemId, selectedCategoryIds);
                    AddFoodTypeWithMenuItem(model.MenuItemId, selectedFoodTypeId);

                    _logger.LogInformation("Menu item updated successfully: {ItemName}", model.ItemName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating menu item: {ItemName}", model.ItemName);
                return false;
            }
        }

        public void AddQuantityWithMenuItem(int menuItemId, List<int> selectedQuantityIds)
        {
            var linkingToDelete = _context.MenuItemQuantity
            .Where(q => q.MenuItemId == menuItemId)
            .ToList();

            _context.MenuItemQuantity.RemoveRange(linkingToDelete);
            _context.SaveChanges();

            foreach (int quantityId in selectedQuantityIds)
            {
                var menuItemQuantity = new MenuItemQuantity
                {
                    MenuItemId = menuItemId,
                    QuantityId = quantityId
                };
                _context.MenuItemQuantity.Add(menuItemQuantity);
                _context.SaveChanges();
            }
        }
        public void AddFoodTypeWithMenuItem(int menuItemId, int selectedFoodTypeId)
        {
            //var linkingToDelete = _context.MenuItemFoodType
            //.Where(q => q.MenuItemId == menuItemId)
            //.FirstOrDefault();

            //_context.MenuItemFoodType.Remove(linkingToDelete);
            //_context.SaveChanges();

            //var menuItemFoodType = new MenuItemFoodType
            //{
            //    MenuItemId = menuItemId,
            //    FoodTypeId = selectedFoodTypeId,
            //    IsDeleted = false
            //};
            //_context.MenuItemFoodType.Add(menuItemFoodType);

            var menuItem = new MenuItem
            {
                MenuItemId = menuItemId,
                FoodTypeId = selectedFoodTypeId
            };
            _context.MenuItems.Update(menuItem);
            _context.SaveChanges();
        }
        public async Task<MenuItem> GetMenuItemById(int id)
        {
            try
            {
                var result = await (from m in _context.MenuItems
                                    join mic in _context.MenuItemCategories on m.MenuItemId equals mic.MenuItemId
                                    join mc in _context.MenuCategories on mic.MenuCategoryId equals mc.MenuCategoryId
                                   //join mift in _context.MenuItemFoodType on m.MenuItemId equals mift.MenuItemId
                                   join ft in _context.FoodType on m.FoodTypeId equals ft.Id
                                   where mc.IsActive == true && ft.IsDeleted == false && m.MenuItemId == id
                                    group new { m, mc, ft } by m into g
                                    //group m by m.MenuItemId into g
                                    select new MenuItem
                                    {
                                        MenuItemId = id,
                                        ItemName = g.Key.ItemName,
                                        Description = g.Key.Description,
                                        Price = g.Key.Price,
                                        IsVeg = g.Key.IsVeg,
                                        IsAvailable = g.Key.IsAvailable,
                                        ImageUrl = g.Key.ImageUrl,
                                        MenuCategories = g
                                            .Select(x => new MenuCategory
                                            {
                                                MenuCategoryId = x.mc.MenuCategoryId,
                                                CategoryName = x.mc.CategoryName,
                                                Description = x.mc.Description,
                                                DisplayOrder = x.mc.DisplayOrder,
                                                IsActive = x.mc.IsActive,
                                                StartDate = x.mc.StartDate,
                                                EndDate = x.mc.EndDate
                                            }).ToList(),
                                        FoodTypeId = g.Key.FoodTypeId,
                                        FoodType = g
                                            .Select(x => new FoodType
                                            {
                                                Id = x.ft.Id,
                                                Name = x.ft.Name,
                                                Description = x.ft.Description
                                            }).FirstOrDefault()
                                    }
                                ).FirstOrDefaultAsync();

                return result ?? new MenuItem
                {
                    MenuItemId = id,
                    ItemName = string.Empty,
                    Description = string.Empty,
                    Price = 0,
                    IsVeg = false,
                    IsAvailable = false,
                    ImageUrl = string.Empty,
                    MenuCategories = new List<MenuCategory>(),
                    FoodTypeId = -1,
                    FoodType = new FoodType()
                };

                //return await _context.MenuItems
                //    .AsNoTracking()
                //    .FirstOrDefaultAsync(m => m.MenuItemId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving menu item by ID {id}");
                //return new MenuItem();
                return new MenuItem
                {
                    MenuItemId = id,
                    ItemName = string.Empty,
                    Description = string.Empty,
                    Price = 0,
                    IsVeg = false,
                    IsAvailable = false,
                    ImageUrl = string.Empty,
                    MenuCategories = new List<MenuCategory>(),
                    FoodType = new FoodType()
                };
            }
        }
        public async Task<List<MenuItemQuantity>> GetQuantityById(int menuItemId)
        {
            try
            {
                return await _context.MenuItemQuantity
                    .Where(m => m.MenuItemId == menuItemId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving menu item by ID {menuItemId}");
                return new List<MenuItemQuantity>();
            }
        }
        public void AddCategoryWithMenuItem(int menuItemId, List<int> selectedCategoryIds)
        {
            var linkingToDelete = _context.MenuItemCategories
            .Where(q => q.MenuItemId == menuItemId)
            .ToList();

            _context.MenuItemCategories.RemoveRange(linkingToDelete);
            _context.SaveChanges();

            foreach (int categoryId in selectedCategoryIds)
            {
                var menuItemCategory = new MenuItemCategories
                {
                    MenuItemId = menuItemId,
                    MenuCategoryId = categoryId,
                    IsDeleted = false
                };
                _context.MenuItemCategories.Add(menuItemCategory);
                _context.SaveChanges();
            }
        }
        public async Task<List<MenuCategory>> GetCategoriesByMenuId(int menuItemId)
        {
            try
            {
                var items =  await (from mic in _context.MenuItemCategories
                    join mc in _context.MenuCategories on mic.MenuCategoryId equals mc.MenuCategoryId
                    where mc.IsActive == true && mic.MenuItemId == menuItemId
                    select mc
                    ).ToListAsync();
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving menu item by ID {menuItemId}");
                return new List<MenuCategory>();
            }
        }
        public async Task<MenuCategory> GetMenuCategoryById(int id)
        {
            try
            {
                return await _context.MenuCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MenuCategoryId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving menu schedule by ID {id}");
                return new MenuCategory();
            }
        }
        public async Task<bool> AddEditCategoryAsync(MenuCategory model, int? categoryID, List<int> selectedMenuItemIds)
        {
            try
            {
                if (categoryID == 0)
                {
                    var category = new MenuCategory
                    {
                        CategoryName = model.CategoryName,
                        Description = model.Description,
                        IsActive = model.IsActive,
                        DisplayOrder = model.DisplayOrder,
                        RestaurantId = 1,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate
                    };
                    _context.MenuCategories.Add(category);
                    _context.SaveChanges();
                    int generatedId = category.MenuCategoryId;

                    AddMenuItemWithCategory(generatedId, selectedMenuItemIds);

                    _logger.LogInformation("Schedule added successfully: {CategoryName}", model.CategoryName);
                    return true;
                }
                else
                {
                    var category = new MenuCategory
                    {
                        MenuCategoryId = model.MenuCategoryId,
                        CategoryName = model.CategoryName,
                        Description = model.Description,
                        IsActive = model.IsActive,
                        DisplayOrder = model.DisplayOrder,
                        RestaurantId = 1,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate
                    };
                    _context.MenuCategories.Update(category);
                    _context.SaveChanges();

                    AddMenuItemWithCategory(model.MenuCategoryId, selectedMenuItemIds);

                    _logger.LogInformation("Schedule updated successfully: {CategoryName}", model.CategoryName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating schedule: {CategoryName}", model.CategoryName);
                return false;
            }
        }
        public void AddMenuItemWithCategory(int categoryId, List<int> selectedMenuItemIds)
        {
            var linkingToDelete = _context.MenuItemCategories
            .Where(q => q.MenuCategoryId == categoryId)
            .ToList();

            _context.MenuItemCategories.RemoveRange(linkingToDelete);
            _context.SaveChanges();

            foreach (int menuItemId in selectedMenuItemIds)
            {
                var menuItemCategory = new MenuItemCategories
                {
                    MenuItemId = menuItemId,
                    MenuCategoryId = categoryId,
                    IsDeleted = false
                };
                _context.MenuItemCategories.Add(menuItemCategory);
                _context.SaveChanges();
            }
        }
        //public async Task<List<OrderStatusMaster>> GetAllOrderStatus()
        //{
        //    try
        //    {
        //        var orderStatusList = await _context.OrderStatusMasters
        //            .AsNoTracking()
        //            .Where(c => c.IsActive == true)
        //            .ToListAsync();
        //        _logger.LogInformation("Retrieved {Count} OrderStatusMasters", orderStatusList.Count);
        //        return orderStatusList;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving menu categories");
        //        return new List<OrderStatusMaster>();
        //    }
        //}
        public async Task<bool> DeleteCategoryById(int id)
        {
            try
            {
                var category = await _context.MenuCategories.FirstOrDefaultAsync(m => m.MenuCategoryId == id);
                if (category == null)
                {
                    _logger.LogWarning($"Delete schedule failed: No category found with ID {id}");
                    return false;
                }
                category.IsDeleted = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Schedule deleted successfully for {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting schedule by ID {id}");
                return false;
            }
        }
        public async Task<bool> DeleteMenuItemById(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.MenuItemId == id);
                if (menuItem == null)
                {
                    _logger.LogWarning($"Delete menu item failed: No menu item found with ID {id}");
                    return false;
                }
                menuItem.IsDeleted = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Menu item deleted successfully for {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting schedule by ID {id}");
                return false;
            }
        }
        public async Task<PickUpTime> GetPickupTimeById(int id)
        {
            try
            {
                var result = await _context.PickUpTime
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (result == null)
                    return new PickUpTime();
                else
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving pickup time by ID {id}");
                return new PickUpTime();
            }
        }
        public async Task<bool> AddEditPickupTimeAsync(PickUpTime model, int? id)
        {
            try
            {
                if (id == 0)
                {
                    var pickupTime = new PickUpTime
                    {
                        Time = model.Time,
                        IsActive = model.IsActive
                    };
                    _context.PickUpTime.Add(pickupTime);
                    _context.SaveChanges();
                    int generatedId = pickupTime.Id;

                    _logger.LogInformation("PickUpTime added successfully: {Time}", model.Time);
                    return true;
                }
                else
                {
                    var pickupTime = new PickUpTime
                    {
                        Id = model.Id,
                        Time = model.Time,
                        IsActive = model.IsActive
                    };
                    _context.PickUpTime.Update(pickupTime);
                    _context.SaveChanges();

                    _logger.LogInformation("PickUpTime updated successfully: {Time}", model.Time);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating PickUpTime: {Time}", model.Time);
                return false;
            }
        }
        public async Task<bool> DeletePickupTimeById(int id)
        {
            try
            {
                var pickupTime = await _context.PickUpTime.FirstOrDefaultAsync(p => p.Id == id);
                if (pickupTime == null)
                {
                    _logger.LogWarning($"Delete pickup time failed: No pickup time found with ID {id}");
                    return false;
                }
                _context.PickUpTime.Remove(pickupTime);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Pickup time deleted successfully for {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting pickup time by ID {id}");
                return false;
            }
        }
        public async Task<bool> InActiveMenuCategories()
        {
            try
            {
                var today = DateTime.Now;
                var items = await _context.MenuCategories
                    .Where(c => c.IsActive == true && c.EndDate < today && c.MenuCategoryId != 2)
                    .ToListAsync();
                if(items != null)
                {
                    foreach (var item in items)
                    {
                        item.IsActive = false;
                        _context.MenuCategories.Update(item);
                        _context.SaveChanges();
                    }                    
                }
                else
                {
                    _logger.LogWarning($"Not found category for making inactive.");
                    return false;
                }
                _logger.LogInformation("InActive Menu Categories successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in InActiveMenuCategories function");
                return false;
            }
        }
    }
}