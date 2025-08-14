using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using FoodDeliveryApp.Models;
using FoodDeliveryApp.Services;
using FoodDeliveryApp.ViewModels.Customers;
using FoodDeliveryApp.ViewModels.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using System.Security.Principal;
using Microsoft.SqlServer.Server;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using FoodDeliveryApp.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using FoodDeliveryApp.ViewModels;
using Microsoft.AspNetCore.Http;

namespace FoodDeliveryApp.Controllers
{
    public class CustomerAccountController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerAccountController> _logger;
        private readonly ICommonService _commonService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private const string SessionCartKey = "Cart";

        public CustomerAccountController(
        ICustomerService customerService,
        ILogger<CustomerAccountController> logger,
        IEmailService emailService,
        IMemoryCache cache,
        ICommonService commonService)
        {
            _customerService = customerService;
            _logger = logger;
            _emailService = emailService;
            _commonService = commonService;
            _cache = cache;
        }

        // Public Landing Page
        public IActionResult Index()
        {
            // If the user is already authenticated, redirect to Food Search.
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("FoodSearch", "CustomerAccount");
            }
            return View();
        }
        // GET: Customer Registration
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Process Customer Registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(CustomerRegisterViewModel model)
        {
            try
            {
                // Pre-check for duplicate email and phone number
                if (await _commonService.IsEmailDuplicateAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "A user with this email already exists. Please use a di");
                }
                if (await _commonService.IsPhoneDuplicateAsync(model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "A user with this phone number already exists. Pl");
                }

                if (!ModelState.IsValid)
                    return View(model);

                var result = await _customerService.RegisterCustomerAsync(model);
                if (result)
                {
                    // After registration, auto-sign in the user.
                    var user = await _customerService.LoginCustomerAsync(new LoginViewModel { Email = model.Email, Password = model.Password });
                    if (user != null)
                    {
                        await SignInUser(user, rememberMe: false);
                        //return RedirectToAction("Welcome", "CustomerAccount");
                        return RedirectToAction("FoodSearch", "CustomerAccount");
                    }
                }
                ViewBag.ErrorMessage = "Registration failed. Please try again.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during registration for email {Email}", model.Email);
                ViewBag.ErrorMessage = "An error occurred during registration. Please try again later.";
                return View(model);
            }
        }

        // GET: Welcome page after registration
        //[Authorize(Roles = "Customer")]
        public IActionResult Welcome()
        {
            return View();
        }

        // GET: Customer Login Page
        [HttpGet]
        public IActionResult Login(string? ReturnUrl = null)
        {
            var customerLoginViewModel = new LoginViewModel()
            {
                ReturnUrl = ReturnUrl
            };
            return View(customerLoginViewModel);
        }

        // POST: Process Customer Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);
                var user = await _customerService.LoginCustomerAsync(model);
                if (user != null)
                {
                    await SignInUser(user, model.RememberMe);
                    // Check if the ReturnUrl is not null and is a local URL
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        // Redirect to default page
                        return RedirectToAction("FoodSearch", "CustomerAccount");
                    }
                }
                ViewBag.ErrorMessage = "Invalid email or password";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login for email {Email}", model.Email);
                ViewBag.ErrorMessage = "An error occurred during login. Please try again later.";
                return View(model);
            }
        }

        // GET: Forgot Password Page
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Process Forgot Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var result = await _customerService.ForgotPasswordAsync(model);
                if (result)
                {
                    // Generate a secure token and store it in cache (expires in 15 minutes)
                    string token = Guid.NewGuid().ToString();
                    _cache.Set(model.Email, token, TimeSpan.FromMinutes(15));
                    // Build the reset link using safe URL generation.
                    string resetLink = Url.Action("ResetPassword", "CustomerAccount", new { token, email = model.Email }, protocol: Request.Scheme) ?? string.Empty;

                    string subject = "Reset Your Password Food Delivery App";
                    string body = @"<div style='font-family: Arial, sans-serif;'>
                                    <h2 style='color: #2e6c80;'>Password Reset Request</h2>
                                    <p>Hello,</p>
                                    <p>We received a request to reset your password. Click the button below</p>
                                    <p><a href='" + resetLink + @" style='color: #ffffff;background-color:'</p>
                                    <p>If you did not request this, please ignore this email.</p>
                                    <p>Thank you, <br/>Food Delivery App Team</p>
                                    </div>";

                    await _emailService.SendEmailAsync(model.Email, subject, body, true);
                    ViewBag.Message = "A password reset link has been sent to your email address.";
                    return View("Forgot Password Confirmation");
                }
                ModelState.AddModelError("", "Failed to send reset link. Please check your email and try again");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during forgot password for email {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred. Please try again later.");
                return View(model);
            }
        }

        // GET: Reset Password Page
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Invalid password reset token.");
                return View("Error");
            }
            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };
            return View(model);
        }

        // POST: Process Reset Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);
                // Validate token stored in cache.
                if (!_cache.TryGetValue(model.Email, out string? storedToken) || storedToken != model.Token)
                {
                    ViewBag.ErrorMessage = "Invalid or expired password reset token.";
                    return View(model);
                }

                var result = await _customerService.ResetPasswordAsync(model);
                if (result)
                {
                    // Remove token after successful reset.
                    _cache.Remove(model.Email);

                    string subject = "Your Password Has Been Reset - Food Delivery App";
                    string body = @"<div style='font-family: Arial, sans-serif;'>
                                <h2 style='color: # 2e6c80;'>Password Reset Confirmation</h2>
                                <p>Hello,</p>
                                <p>Your password has been successfully reset. If you did not initiate
                                <p>Thank you, <br/>Food Delivery App Team</p>
                                </div>";
                    await _emailService.SendEmailAsync(model.Email, subject, body, true);
                    return View("ResetPasswordConfirmation");
                }

                ModelState.AddModelError("", "Password reset failed. Please ensure the link is valid or try again");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during reset password for email {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred while resetting your password. Please try again");
                return View(model);
            }
        }

        // GET: Change Password Page
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Process Change Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                // Retrieve user ID from claims safely.
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var result = await _customerService.ChangePasswordAsync(userId, model);
                if (result)
                {
                    string subject = "Your Password Has Been Changed Food Delivery App";
                    string body = @"<div style='font-family: Arial, sans-serif;'>
                                        <h2 style='color: #2e6c80;'>Password Change Confirmation</h2>
                                        <p>Hello,</p>
                                        <p>Your password has been successfully changed. If you did not make this char
                                        <p>Thank you,<br/>Food Delivery App Team</p>
                                        </div>";

                    // Retrieve user's email from the identity.
                    string? userEmail = User?.Identity?.Name;
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        return RedirectToAction("Login");
                    }
                    await _emailService.SendEmailAsync(userEmail, subject, body, true);
                    ViewBag.Message = "Password changed successfully.";
                }
                ViewBag.ErrorMessage = "Change password failed. Please ensure your current password is correct";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during change password");
                ViewBag.ErrorMessage = "An error occured while changing your password. Please try again.";
                return View(model);
            }
        }

        // Logout Action
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "CustomerAccount");
        }

        // GET: Add Customer Address
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public IActionResult AddAddress()
        {
            return View();
        }

        // POST: Process Add Customer Address
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddAddress(AddressViewModel model)
        {
            try
            {
                // Validate the input view model.
                if (!ModelState.IsValid)
                    return View(model);

                // Retrieve the current user's ID from claims.
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                // Call the servise to save the address
                bool addressSaved = await _customerService.AddAddressAsync(model, userId);
                if (addressSaved)
                {
                    TempData["SuccessMessage"] = "Your new address has been added successfully and is now available.";
                    return RedirectToAction("ManageAddresses");
                }
                ModelState.AddModelError("", "Failed to add address. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during address addition.");
                ModelState.AddModelError("", "An error occured while adding your address. Please try again later.");
                return View(model);
            }
        }

        // GET: Manage Customer Addresses
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ManageAddresses()
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var addresses = await _customerService.GetAddressesAsync(userId);
                return View(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during address management.");
                return View("Error");
            }
        }

        // GET: Edit Customer Address
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> EditAddress(int id)
        {
            try
            {
                // Retrieve the current user ID.
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                // Retrieve the address for the current user.
                var address = await _customerService.GetAddressByIdAsync(userId, id);
                if (address == null)
                {
                    _logger.LogInformation($"Address not found for updation. Address ID: {id}");
                    TempData["ErrorMessage"] = $"Address not found for updation. Address ID: {id}";
                    return RedirectToAction("ManageAddresses");
                }

                // Map the UserAddress data model to the AddressViewModel.
                var model = new EditAddressViewModel
                {
                    UserAddressId = address.UserAddressId,
                    Label = address.Label,
                    AddressLine1 = address.AddressLine1,
                    AddressLine2 = address.AddressLine2,
                    City = address.City,
                    State = address.State,
                    ZipCode = address.ZipCode,
                    Landmark = address.Landmark,
                    Latitude = address.Latitude,
                    Longitude = address.Longitude
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception fetching address for editing. Address ID: {AddressId}", id);
                TempData["ErrorMessage"] = $"Exception fetching address for editing. Address ID: {id}";
                return View("Error");
            }
        }

        // POST: Process Edit Customer Address
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> EditAddress(EditAddressViewModel model)
        {
            try
            {
                // Validate the input view model.
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Retrieve the current user's ID from claims.
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                // Update the address via the service.
                bool updateResult = await _customerService.UpdateAddressAsync(model, userId);
                if (updateResult)
                {
                    TempData["SuccessMessage"] = "Your address details have been updated successfully.";
                    return RedirectToAction("ManageAddresses");
                }
                ModelState.AddModelError("", "Failed to update address. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during address update. Address ID: {AddressId}", model.UserAddressId);
                ModelState.AddModelError("", "An error occured while updating your address. Please try again later.");
                return View(model);
            }
        }

        // GET: Delete Customer Address (Confirmation Page)
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                // Retrieve the current user's ID from claims.
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var address = await _customerService.GetAddressByIdAsync(userId, id);
                if (address == null)
                {
                    _logger.LogInformation($"Address not found for deletion. Address ID: {id}");
                    TempData["ErrorMessage"] = $"Address not found for deletion. Address ID: {id}";
                    return RedirectToAction("ManageAddresses");
                }
                return View(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception fetching address for deletion. AddressID: {AddressId}", id);
                TempData["ErrorMessage"] = $"Exception fetching address for deletion. AddressID: {id}";
                return View("Error");
            }
        }

        // POST: Process Delete Customer Address
        [HttpPost, ActionName("DeleteAddress")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteAddressConfirmed(int id)
        {
            try
            {
                // Retrieve the current user's ID from claims.
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                bool deleteResult = await _customerService.DeleteAddressAsync(userId, id);
                if (deleteResult)
                {
                    TempData["SuccessMessage"] = $"The address has been removed from your account.";
                    return RedirectToAction("ManageAddresses");
                }

                _logger.LogInformation($"Failed to delete address. Please try again. AddressId: {id}");
                TempData["ErrorMessage"] = $"Failed to delete address. Please try again. AddressId: {id}";
                return RedirectToAction("ManageAddresses");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception deleting address. AddressID: {AddressId}", id);
                return View("Error");
            }
        }

        //// GET: Dummy My Account Page temp
        //[Authorize(Roles = "Customer")]
        //public IActionResult MyAccount()
        //{
        //    return View();
        //}

        // GET: My Account - Displays Customer information
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyAccount()  // Need to update
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var account = await _customerService.GetCustomerByIdAsync(userId);
                return View(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during address management.");
                return View("Error");
            }
        }

        // GET: Edit Account - Displays Customer information
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> EditAccount() // Need to update
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var account = await _customerService.GetCustomerByIdAsync(userId);
                if (account == null)
                {
                    _logger.LogInformation($"Account not found for updation. UserID: {userIdStr}");
                    TempData["ErrorMessage"] = $"Account not found for updation. UserID: {userIdStr}";
                    return RedirectToAction("MyAccount");
                }

                // Map the User data model to the AccountViewModel.
                var model = new AccountViewModel
                {
                    FirstName = account.FirstName,
                    LastName = account.LastName,
                    Email = account.Email,
                    PhoneNumber = account.PhoneNumber,
                    UserId = account.UserId
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching account deatils for editing.");
                TempData["ErrorMessage"] = "Exception while fetching account deatils for editing.";
                return View("Error");
            }
        }

        // POST: Edit Account - Processes the account update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> EditAccount(AccountViewModel model)
        {
            // Validate the Phone Number
            var IsValid = await _commonService.IsPhoneNumberAvailableAsync(model.PhoneNumber, model.UserId);
            if (IsValid)
            {
                ModelState.AddModelError("PhoneNumber", "Phone Number is already in use.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                bool updateResult = await _customerService.UpdateCustomerAccountAsync(model);
                if (updateResult)
                {
                    // Set a success message to be displayed on the MyAccount page.
                    TempData["SuccessMessage"] = "Your account information has been updated successfully.";
                    return RedirectToAction("MyAccount");
                }
                ViewBag.ErrorMessage = "Update failed. Please try again.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer account for user {UserId}", model.UserId);
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View(model);
            }
        }


        // Helper method to sign in the user using cookie authentication.
        private async Task SignInUser(User user, bool rememberMe)
        {
            // Create a list of claims including the email and user ID.
            var claims = new List<Claim>
                {
                    // ClaimTypes.Name is typically used to store the email or username.
                    new Claim(ClaimTypes.Name, user.Email),
                    // You can add custom claims such as "UserId".
                    new Claim("UserId", user.UserId.ToString()),
                    //Storing the Customer Role in the Role Claims
                    new Claim(ClaimTypes.Role, "Customer")
                };
            // Create the claims identity with the specified authentication scheme.
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Set authentication properties including whether to persist the cookie.
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            };

            // Sign in the user by creating an authentication cookie that stores the claims.
            await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
        }

        // GET: Dummy Food Search Page after login
        //[Authorize(Roles = "Customer")]
        public async Task<IActionResult> FoodSearch()
        {
            //return View();
            try
            {
                //var userIdStr = User?.FindFirst("UserId")?.Value;
                //if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                //{
                //    return RedirectToAction("Login");
                //}
                var categories = await _customerService.GetAllMenuCategory();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during showing menu category.");
                return View("Error");
            }
        }

        // GET: Menu Items
        [HttpGet]
        //[Authorize(Roles = "Customer")]
        //public async Task<IActionResult> FoodItems(int id, MenuCategory category)
        public async Task<IActionResult> FoodItems(int id)
        {
            try
            {
                // Retrieve the current user ID.
                //var userIdStr = User?.FindFirst("UserId")?.Value;
                //if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                //{
                //    return RedirectToAction("Login");
                //}

                // Retrieve the menu items for the selected category.
                var items = await _customerService.GetAllMenuItems(id);
                ViewData["QuantityList"] = await _commonService.GetAllQuantity();
                ViewData["SubQuantityList"] = await _commonService.GetAllSubQuantity();
                ViewData["SelectedCategoryId"] = id;

                if (items == null)
                {
                    _logger.LogInformation($"Menu items not found for category ID: {id}");
                    TempData["ErrorMessage"] = $"Menu items not found for category ID: {id}";
                    return RedirectToAction("FoodSearch");
                }
                return View(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception fetching menu items for category ID: {id}");
                TempData["ErrorMessage"] = $"Exception fetching menu items for category ID: {id}";
                return View("Error");
            }
        }

        // POST: Add item to cart
        [HttpPost]
        //[Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(int menuItemId, string quantity, int selectedCategoryId, int subQuantity)
        {
            try
            {
                // Retrieve the current user ID.
                //var userIdStr = User?.FindFirst("UserId")?.Value;
                //if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                //{
                //    return RedirectToAction("Login");
                //}               

                // Retrieve the menu item by ID.
                var item = await _customerService.GetMenuItemById(menuItemId);
                if (item == null)
                    return NotFound();

                // Add item to the order.
                //var cart = HttpContext.Session.GetObject<List<OrderItem>>(SessionCartKey) ?? new List<OrderItem>();
                var cart = HttpContext.Session.GetObject<OrderItemList>(SessionCartKey) ?? new OrderItemList();

                // Check if the item already exists in the cart.
                if (cart.Items == null)
                {
                    cart.Items = new List<OrderItem>();
                }
                var orderItem = cart.Items.FirstOrDefault(o => o.MenuItemId == menuItemId);
                if (orderItem != null)
                {
                    orderItem.Quantity = quantity;
                    orderItem.SubQuantity = subQuantity;
                }
                else
                {
                    decimal totalPrice;
                    int parshedValue;
                    bool isInt = int.TryParse(quantity, out parshedValue);
                    if(isInt)
                    {
                        totalPrice = item.Price * parshedValue;
                    }
                    else
                    {
                        totalPrice = item.Price;
                        if (quantity == "Half")
                        {
                            totalPrice = (item.Price / 2) * subQuantity;
                        }
                        if (quantity == "Quarter")
                        {
                            totalPrice = (item.Price / 4) * subQuantity;
                        }
                    }

                    cart.Items.Add(new OrderItem { MenuItemId = menuItemId, MenuItem = item, Quantity = quantity, UnitPrice = item.Price, TotalPrice = totalPrice, SubQuantity = subQuantity });
                }
                cart.CategoryId = selectedCategoryId;

                HttpContext.Session.SetObject(SessionCartKey, cart);

                if (cart.Items.Count>0)
                {
                    TempData["SuccessMessage"] = "Item(s) added successfully to the cart.";
                }

                return RedirectToAction("Cart", "CustomerAccount");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception adding item to the cart for item ID: {menuItemId}");
                TempData["ErrorMessage"] = $"Exception adding item to the cart for item ID: {menuItemId}";
                return View("Error");
            }
        }

        // GET: Selected food items to order
        //[Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cart()
        {
            //return View();
            try
            {
                //var userIdStr = User?.FindFirst("UserId")?.Value;
                //if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                //{
                //    return RedirectToAction("Login");
                //}

                //var cart = HttpContext.Session.GetObject<List<OrderItem>>(SessionCartKey) ?? new List<OrderItem>();
                var cart = HttpContext.Session.GetObject<OrderItemList>(SessionCartKey) ?? new OrderItemList();
                var pickupTimes = await _customerService.GetPickupTimes();
                cart.PickUpTimes = pickupTimes;
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during showing menu category.");
                return View("Error");
            }
        }

        // GET: Delete cart items
        [HttpGet]
        //[Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteCart(int menuItemId)
        {
            try
            {
                // Retrieve the current user ID.
                //var userIdStr = User?.FindFirst("UserId")?.Value;
                //if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                //{
                //    return RedirectToAction("Login");
                //}

                // Retrieve the cart from the session.
                //var cart = HttpContext.Session.GetObject<List<OrderItem>>(SessionCartKey) ?? new List<OrderItem>();
                var cart = HttpContext.Session.GetObject<OrderItemList>(SessionCartKey) ?? new OrderItemList();
                cart.Items.RemoveAll(item => item.MenuItemId == menuItemId);

                //HttpContext.Session.Clear();
                HttpContext.Session.Remove(SessionCartKey);
                HttpContext.Session.SetObject(SessionCartKey, cart);

                return RedirectToAction("Cart", "CustomerAccount");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during deleting session for cart item(s).");
                TempData["ErrorMessage"] = $"Exception during deleting session for cart item(s).";
                return View("Error");
            }
        }

        // Create Order
        //[Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder(OrderItemList orderItemsList, string pickupTime, int categoryId)
        {
            try
            {
                // Retrieve the current user ID.
                //var userIdStr = User?.FindFirst("UserId")?.Value;
                //if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                //{
                //    return RedirectToAction("Login");
                //}

                int userId = -1;

                //HttpContext.Session.SetObject("Customer_FirstName", orderItemsList.FirstName);
                //HttpContext.Session.SetObject("Customer_LastName", orderItemsList.LastName);
                //HttpContext.Session.SetObject("Customer_EmailAddress", orderItemsList.EmailAddesss);
                //HttpContext.Session.SetObject("Customer_PhoneNumber", orderItemsList.PhoneNumber);

                HttpContext.Session.SetString("Customer_FirstName", orderItemsList.FirstName.ToString());
                HttpContext.Session.SetString("Customer_LastName", orderItemsList.LastName.ToString());
                HttpContext.Session.SetString("Customer_EmailAddress", orderItemsList.EmailAddesss.ToString());
                HttpContext.Session.SetString("Customer_PhoneNumber", orderItemsList.PhoneNumber.ToString());

                //Create the order
                var order = await _customerService.CreateOrder(orderItemsList.Items, userId, pickupTime, orderItemsList.CategoryId, orderItemsList.FirstName, orderItemsList.LastName, orderItemsList.PhoneNumber, orderItemsList.EmailAddesss, orderItemsList.Comments);

                if ((order != null) && (order.OrderId != 0))
                {
                    //HttpContext.Session.Clear();
                    HttpContext.Session.Remove(SessionCartKey);
                    _logger.LogInformation($"Order created successfully.");
                    TempData["SuccessMessage"] = $"Order created successfully. Your order number is " + order.OrderCode;
                }
                else
                {
                    _logger.LogInformation($"Order creation failed. Please try again.");
                    TempData["ErrorMessage"] = $"Order creation failed. Please try again.";
                }
                return RedirectToAction("FoodSearch", "CustomerAccount");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while creating order.");
                TempData["ErrorMessage"] = $"Exception while creating order.";
                return View("Error");
            }
        }
        public async Task<IActionResult> Orders(OrderItemList? orderInfo)
        {
            try
            {
                //var userIdStr = User?.FindFirst("UserId")?.Value;
                //if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                //{
                //    return RedirectToAction("Login");
                //}

                if ((orderInfo != null) && (orderInfo.FirstName != null))
                {
                    var orders = await _customerService.GetAllCustomerOrders(orderInfo.FirstName, orderInfo.LastName, orderInfo.PhoneNumber, orderInfo.EmailAddesss, -1);
                    var ordersObj = new Orders();
                    ordersObj.OrderList = orders;
                    ordersObj.OrderStatusList = await _commonService.GetAllOrderStatus();
                    return View(ordersObj);
                }
                else
                {
                    _logger.LogInformation($"Order not found for customer.");
                    TempData["ErrorMessage"] = $"Order not found for customer.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during showing customer orders.");
                return View("Error");
            }
        }
        public async Task<IActionResult> OrdersDetails()
        {
            try
            {
                //var userIdStr = User?.FindFirst("UserId")?.Value;
                //if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                //{
                //    return RedirectToAction("Login");
                //}

                bool exists = HttpContext.Session.GetString("Customer_FirstName") != null;

                if (exists)
                {
                    var firstName = HttpContext.Session.GetString("Customer_FirstName") ?? "";
                    var lastName = HttpContext.Session.GetString("Customer_LastName") ?? "";
                    var emailAddress = HttpContext.Session.GetString("Customer_EmailAddress") ?? "";
                    var phoneNumber = HttpContext.Session.GetString("Customer_PhoneNumber") ?? "";

                    var orders = await _customerService.GetAllCustomerOrders(firstName, lastName, phoneNumber, emailAddress, -1);
                    var ordersObj = new Orders();
                    ordersObj.OrderList = orders;
                    ordersObj.OrderStatusList = await _commonService.GetAllOrderStatus();
                    return View("Orders", ordersObj);
                }
                else
                {
                    var order = new OrderItemList();
                    return View("InfoForOrder", order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during OrdersDetails controller function for customer.");
                return View("Error");
            }
        }

    }
}