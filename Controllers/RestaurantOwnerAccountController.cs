using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using FoodDeliveryApp.Services;
using FoodDeliveryApp.Models;
using Microsoft.Extensions.Caching.Memory;
using FoodDeliveryApp.Models;
using FoodDeliveryApp.ViewModels.RestaurantOwners; 
using FoodDeliveryApp.ViewModels.Common;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Security.Principal;
using System.Linq.Expressions;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Common;
using FoodDeliveryApp.Extensions;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FoodDeliveryApp.ViewModels;
using static NuGet.Packaging.PackagingConstants;

namespace FoodDeliveryApp.Controllers
{

    public class RestaurantOwnerAccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IRestaurantOwnerService _ownerService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RestaurantOwnerAccountController> _logger;
        private readonly ICommonService _commonService;
        private readonly IWebHostEnvironment _env;

        public RestaurantOwnerAccountController(
            IConfiguration configuration,
            IRestaurantOwnerService ownerService,
            IEmailService emailService,
            IMemoryCache cache,
            ILogger<RestaurantOwnerAccountController> logger,
            ICommonService commonService,
            IWebHostEnvironment env)
        {
            _configuration = configuration;
            _ownerService = ownerService;
            _emailService = emailService;
            _cache = cache;
            _logger = logger;
            _commonService = commonService;
            _env = env;
        }

        // Displays the initial page for Restaurant Owner signup/signin.
        // If the user is already authenticated, redirects them based on verification status.

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // If the user is already authenticated, decide where to redirect them.
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var userIdStr = User?.FindFirst("UserId")?.Value;

                    // If userIdStr is invalid, prompt them to log in again.
                    if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    {
                        return RedirectToAction("Login");
                    }

                    // If account is verified, go to Dashboard, else go to AccountVerification
                    if (await _ownerService.IsAccountVerifiedAsync(userId))
                        return RedirectToAction("Dashboard");
                    else
                        return RedirectToAction("AccountVerification");

                }

                // If user is not authenticated, show the landing page
                //return View();
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Index method");
                // Optionally display an error page or a generic message
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View("Error");
            }
        }

        // GET: Registration page for Restaurant Owners
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Processes the Restaurant Owner registration.

        // Includes duplicate email/phone checking and optional auto-login upon success.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RestaurantOwnerRegisterViewModel model)
        {
            try
            {
                // First check if model state is valid before proceeding
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Check if the email is already used
                if (await _commonService.IsEmailDuplicateAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "A user with this email already exists. Please try with another email.");
                }

                // Check if the phone number is already used
                if (await _commonService.IsPhoneDuplicateAsync(model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "A user with this phone number already exists. Please try with another phone number.");
                }

                // If validation failed, redisplay the form with error message
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Attempt to register the user
                bool result = await _ownerService.RegisterRestaurantOwnerAsync(model);
                if (result)
                {
                    // Optionally, log the user in immediately after registration
                    var loginModel = new LoginViewModel
                    {
                        Email = model.Email,
                        Password = model.Password
                    };
                    var user = await _ownerService.LoginRestaurantOwnerAsync(loginModel);
                    if (user != null)
                    {
                        // Sign in the user
                        await SignInUser(user, rememberMe: false);

                        // Redirect based on their verification status
                        //if (await _ownerService.IsAccountVerifiedAsync(user.UserId))
                        //    return RedirectToAction("Dashboard");
                        //else
                        return RedirectToAction("AccountVerification");
                    }
                }

                // If we got here, registration failed
                ViewBag.ErrorMessage("Registration failed. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during registration for email {Email}", model.Email);
                ViewBag.ErrorMessage("An error occured. Please try again later.");
                return View(model);
            }
        }

        // GET: Login page
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var userIdStr = User?.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                // Return a blank LoginViewModel but preserve the returnUrl if present
                var model = new LoginViewModel { ReturnUrl = returnUrl };
                return View(model);
            }
            else
            {
                return RedirectToAction("Dashboard");
            }            
        }

        // POST: Processes the login form for Restaurant Owners.
        // Verifies user credentials and sets auth cookies.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                // If data is invalid, show the form again
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Attempt to authenticate
                var user = await _ownerService.LoginRestaurantOwnerAsync(model);
                if (user != null)
                {
                    // Sign user in if credential are correct
                    await SignInUser(user, model.RememberMe);

                    // Redirect based on verification status
                    if (await _ownerService.IsAccountVerifiedAsync(user.UserId))
                        return RedirectToAction("Dashboard");
                    else
                        return RedirectToAction("AccountVerification");
                }

                // Invalid credentials
                ViewBag.ErrorMessage("Invalid email or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login for email {Email}", model.Email);
                ViewBag.ErrorMessage("An error occured during login. Please try again later.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AccountVerification()
        {
            try
            {
                // Retrieve the user ID from claims
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                // Fetch the profile and verify it exists
                var profile = await _ownerService.GetRestaurantOwnerProfileAsync(userId);
                if (profile == null)
                {
                    return RedirectToAction("Login");
                }

                // Construct a view model with verification info
                var viewModel = new RestaurantOwnerAccountVerificationViewModel
                {
                    Email = profile.User.Email,
                    IsVerified = profile.IsVerified,
                    EstimatedVerificationTimemessage = "Your account is under review. Please allow up to 24 hours for verification."
                };

                // If not verified, check if any admin remarks exist
                if (!profile.IsVerified)
                {
                    if (!string.IsNullOrEmpty(profile.AdminRemarks))
                    {
                        viewModel.Message = $"Verification Failed: {profile.AdminRemarks}";
                    }
                    else
                    {
                        viewModel.Message = "Your account is pending verification";
                    }
                }
                else
                {
                    viewModel.Message = "Your account has been successfully verified";
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AccountVerification method.");
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View("Error");
            }
        }

        // GET: The Restaurant Owner's Dashboard
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Retrieve the user ID from claims
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                // If the user is not verified, force them to the verification page
                if (!await _ownerService.IsAccountVerifiedAsync(userId))
                    return RedirectToAction("AccountVerification");

                // Otherwise show the dashboard
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Dashboard method.");
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View("Error");
            }
        }

        // GET: Forgot password page where user can request a reset link.
        [HttpGet]
        public IActionResult ForgotPassowrd()
        {
            return View();
        }

        // POST: Processes the forgot password request.
        // Sends email with reset token if successful.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                // If user input is invalid, redisplay form
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Attempt to generate a reset request
                bool result = await _ownerService.ForgotPasswordAsync(model);
                if (result)
                {
                    // Generate a unique token and store it in memory cache
                    string token = Guid.NewGuid().ToString();
                    _cache.Set(model.Email, token, TimeSpan.FromMinutes(15));

                    // Build the reset link to include in the email
                    string resetLink = Url.Action("ResetPassword", "RestaurantOwnerAccount",
                        new { token, email = model.Email }, Request.Scheme) ?? string.Empty;

                    // Construct the email content
                    string subject = "Reset Your Password - Ruchi Kitchen";
                    string body = @"<!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <style>
                            body { font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }
                            .email-container { max-width: 600px; margin: 30px auto; background: #ffffff; padding: 20px; border: 1px solid; }
                            .header { background: #004085; padding: 20px; color: #ffffff; text-align: center; }
                            .content { margin: 20px 0; line-height: 1.6; }
                            .button { display: inline-block; padding: 12px 25px; background-color: #004085; }
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
                                <p>Dear User,</p>
                                <p>We received a request to reset your password for your Food Delievry App. </p>
                                <p style='text-align:center;'><a class='button' href='" + resetLink + @"'></p>
                                <p>This link will expire in 15 minutes. If you did not request a password reset</p>
                            </div>
                            <div class='footer'>
                                <p>&copy; " + DateTime.Now.Year + @" Ruchi Kitchen. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    <html>";

                    // Send the reset email
                    await _emailService.SendEmailAsync(model.Email, subject, body, true);

                    // Show confirmation page
                    return View("ForgotPasswordConfirmation");
                }

                // If unable to send reset link, inform the user
                ViewBag.ErrorMessage = "Failed to send reset link. Please try again.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during forgot password for email {Email}", model.Email);
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View(model);
            }
        }

        // GET: Displays the Reset Password page. Token and Email are required.
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            // Validate the token and email params
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Invalid token or email.");
                return View("Error");
            }

            // Populate the view model for the reset form
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        // POST: Processes the Reset Password form submission.
        // Verifies the token from the cache and updates the user's password if valid. [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                // If form is invalid, reload the same page
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Check if the token matches what was stored in cache
                if (!_cache.TryGetValue(model.Email, out string? storedToken) || storedToken != model.Token)
                {
                    ViewBag.ErrorMessage = "Invalid or expired token.";
                    return View(model);
                }

                // Attempt to update the password
                bool result = await _ownerService.ResetPasswordAsync(model);
                if (result)
                {
                    // Remove the token from cache after successful reset
                    _cache.Remove(model.Email);
                    return View("ResetPasswordConfirmation");
                }

                // If password update fails, show error
                ViewBag.ErrorMessage = "Passowrd reset failed. Please try again";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during reset password for email {Email}", model.Email);
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View(model);
            }
        }

        // GET: Change password page for logged-in Restaurants Owners.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Processes the Change Password form
        // Verifies current password amd updates to the new one if successful.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {

            try
            {
                // Check if model is valid
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Retrieve current user ID from claims
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                // Attempt to change password
                bool result = await _ownerService.ChangePasswordAsync(userId, model);
                if (result)
                {
                    return View("ChangePasswordConfirmation");
                }

                // If change was unsuccessful, inform user
                ViewBag.ErrorMessage = "Change password failed. Please ensure your current password is correct.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during change password.");
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View(model);
            }
        }

        // GET: Displays the account summary (basic info, address, business details).
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> MyAccount()
        {
            try
            {
                // Retrieve current user ID from claims
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                // Fetch the Restaurant Owner
                var user = await _ownerService.GetRestaurantOwnerByIdAsync(userId);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // Fetch primary address (if any)
                var address = await _ownerService.GetRestaurantOwnerAddressAsync(userId);

                // Fetch business details
                var businessDetails = await _ownerService.GetBusinessDetailsByUserIdAsync(userId);

                // Combine into one view model
                var model = new RestaurantOwnerAccountViewModel
                {
                    // Basic Details
                    UserId = user.UserId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,

                    // Address Details
                    UserAddressId = address?.UserAddressId,
                    AddressLine1 = address?.AddressLine1,
                    AddressLine2 = address?.AddressLine2,
                    City = address?.City,
                    State = address?.State,
                    ZipCode = address?.ZipCode,
                    Landmark = address?.Landmark,

                    // Business Details
                    BusinessLicenseNumber = businessDetails?.BusinessLicenseNumber,
                    GSTIN = businessDetails?.GSTIN,
                    BusinessRegistrationNumber = businessDetails?.BusinessRegistrationNumber
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in MyAccount method.");
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View("Error");
            }
        }

        // GET: Displaying the form for updating basic details (Firstname, Lastname, etc.).
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> UpdateBasicDetails()
        {
            try
            {
                // Retrieve current user ID
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                // Fetch the RestaurantOwner record
                var user = await _ownerService.GetRestaurantOwnerByIdAsync(userId);
                if (user == null) return NotFound();

                // Populate the form model
                var model = new AccountViewModel
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateBasicDetails (Get).");
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View("Error");
            }
        }

        // POST: Updates the Restaurant Owner's basic details.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBasicDetails(AccountViewModel model)
        {
            try
            {
                var IsValid = await _commonService.IsPhoneNumberAvailableAsync(model.PhoneNumber, model.UserId);
                if (IsValid)
                {
                    ModelState.AddModelError("PhoneNumber", "Phone Number is already in use.");
                }

                // Check model validity first
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Retrieve current user ID and attach to the model
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                model.UserId = userId;

                // Call service to update the details in DB
                bool result = await _ownerService.UpdateRestaurantOwnerAccountAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Basic details updated successfully.";
                    return RedirectToAction("MyAccount");
                }

                ViewBag.ErrorMessage = "Failed to update details.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateBasicDetails (Post) for user {UserId}.", model.UserId);
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View("Error");
            }
        }

        // GET: Manage Bank Details, displays current bank information.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> ManageBankDetails()
        {
            // No data is being posted, so no ModelState check needed here.
            var userIdStr = User?.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var bankDetails = await _ownerService.GetBankDetailsByUserIdAsync(userId);
            return View(bankDetails);
        }

        // GET: Displays a form for adding or updating bank details.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> UpdateBankDetails()
        {
            var userIdStr = User?.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login");
            }

            var bankDetails = await _ownerService.GetBankDetailsByUserIdAsync(userId);
            return View(bankDetails);
        }

        // POST: Submits the form for updating bank details.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBankDetails(BankDetailsViewModel model)
        {
            try
            {
                // Validate input first
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Retrieve current user ID
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                model.UserId = userId;

                // Attempt to update
                bool result = await _ownerService.UpdateBankDetailsAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Bank details updated successfully.";
                    return RedirectToAction("ManageBankDetails");
                }

                // If failed, show error
                ViewBag.ErrorMessage = "Failed to update bank details.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateBankDetails (POST) for user {UserId}.", model.UserId);
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View(model);
            }
        }

        // GET: Manage the address of the Restaurant Owner (displays current address if available).
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> ManageAddress()
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var address = await _ownerService.GetRestaurantOwnerAddressAsync(userId);
                return View(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ManageAddress method.");
                ViewBag.ErrorMessage = "An error occured. Please try again later.";
                return View("Error");
            }
        }


        // GET: Form for adding/updating the Restaurant Owner's address.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> AddEditAddress()
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var model = await _ownerService.GetRestaurantOwnerAddressAsync(userId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AddEditAddress (GET).");
                ViewBag.ErrorMessage = "An error occurred. Please try again later.";
                return View("Error");
            }
        }

        // POST: Updates the address information for the Restaurant Owner.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEditAddress(EditAddressViewModel model)
        {
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                bool result = await _ownerService.AddOrUpdateAddressAsync(userId, model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Address updated successfully.";
                    return RedirectToAction("ManageAddress");
                }

                ViewBag.ErrorMessage = "Failed to update address.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AddEditAddress (POST).");
                ViewBag.ErrorMessage = "An error occurred. Please try again later.";
                return View(model);
            }
        }

        // GET: Displays the current business details for the Restaurant Owner.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> ManageBusinessDetails()
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var businessDetails = await _ownerService.GetBusinessDetailsByUserIdAsync(userId);
                return View(businessDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ManageBusinessDetails method.");
                ViewBag.ErrorMessage = "An error occurred. Please try again later.";
                return View("Error");
            }
        }

        // GET: Form for updating business details.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpGet]
        public async Task<IActionResult> UpdateBusinessDetails()
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var businessDetails = await _ownerService.GetBusinessDetailsByUserIdAsync(userId);
                return View(businessDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateBusinessDetails (GET).");
                ViewBag.ErrorMessage = "An error occurred. Please try again later.";
                return View("Error");
            }
        }


        // POST: Updates the Restaurant Owner's business details.
        [Authorize(Roles = "RestaurantOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBusinessDetails(RestaurantOwnerBusinessDetailsViewModel model)
        {
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                model.UserId = userId;
                bool result = await _ownerService.AddOrUpdateBusinessDetailsAsync(model);
                if (result)
                {
                    TempData["SuccessMessage"] = "Business Details updated successfully.";
                    return RedirectToAction("ManageBusinessDetails");
                }

                ViewBag.ErrorMessage = "Failed to update Business Details";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateBusinessDetails (POST).");
                ViewBag.ErrorMessage = "An error occurred. Please try again later.";
                return View("Error");
            }
        }

        // Utility method for signing a user into the application using Cookie Authentication.
        // user: The User object containing ID and Email.
        // rememberMe: Boolean indicating if the login session should persist across browser restarts 3 references
        private async Task SignInUser(User user, bool rememberMe)
        {
            // Create claims containing user info and role
            var claims = new List<Claim>
            {

                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, "RestaurantOwner")
            };

            // Use cookie-based authentication
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = rememberMe });

        }

        // GET: Getting orders details
        [HttpGet]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ManageOrders()
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var orders = await _ownerService.GetAllOrders(userId);
                if (orders == null)
                {
                    _logger.LogInformation($"There are no orders.");
                    TempData["ErrorMessage"] = $"There are no orders.";
                    return RedirectToAction("ManageOrders");
                }
                var ordersObj = new Orders();
                ordersObj.OrderList = orders;
                var allOldOrders = await _ownerService.GetAllOldOrders(userId);
                ordersObj.OldOrderList = allOldOrders;
                ordersObj.OrderStatusList = await _commonService.GetAllOrderStatus();
                return View(ordersObj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching orders.");
                return View("Error");
            }
        }

        // POST: Updating orders details
        [HttpPost]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ManageOrders(int orderId, int orderStatus)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var order = await _ownerService.UpdateOrders(orderId, orderStatus);
                var orderItems = await _ownerService.GetAllOrders(userId);
                if (orderItems == null)
                {
                    _logger.LogInformation($"There are no orders.");
                    TempData["ErrorMessage"] = $"There are no orders.";
                    return RedirectToAction("ManageOrders");
                }
                var ordersObj = new Orders();
                ordersObj.OrderList = orderItems;
                ordersObj.OrderStatusList = await _commonService.GetAllOrderStatus();
                return View(ordersObj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating order.");
                return View("Error");
            }
        }

        // POST: Showing menus
        //[HttpPost]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ManageMenus(int? menuItemId)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var allMenus = await _ownerService.GetAllMenuItemsStored();
                if (allMenus == null)
                {
                    _logger.LogInformation($"There are no menus.");
                    TempData["ErrorMessage"] = $"There are no menus.";
                    return RedirectToAction("ManageMenus");
                }
                return View(allMenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating order.");
                return View("Error");
            }
        }

        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ManageCategories(int? menuCategoryId)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var allMenus = await _ownerService.GetAllMenuCategoriesStored();
                if (allMenus == null)
                {
                    _logger.LogInformation($"There are no category.");
                    TempData["ErrorMessage"] = $"There are no category.";
                    return RedirectToAction("ManageCategories");
                }
                return View(allMenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating order.");
                return View("Error");
            }
        }

        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ManagePickupTime()
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                var pickupTimes = await _ownerService.GetAllPickUpTime();
                if (pickupTimes == null)
                {
                    _logger.LogInformation($"There are no pickup time.");
                    TempData["ErrorMessage"] = $"There are no pickup time.";
                    return RedirectToAction("ManagePickupTime");
                }
                return View(pickupTimes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while managing pickup time.");
                return View("Error");
            }
        }

        //[HttpPost]
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ShowMenuItem(int menuItemId)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                MenuItem menuItem;
                if (menuItemId != -1)
                    menuItem = await _ownerService.GetMenuItemById(menuItemId);
                else
                    menuItem = new MenuItem(); // Create a new instance for adding a new item
                MenuCategoryItemsList menuCategoryItemsList = new MenuCategoryItemsList();
                menuCategoryItemsList.MenuCategories = await _ownerService.GetAllMenuCategoryStored();
                menuCategoryItemsList.MenuItems.Add(menuItem);
                menuCategoryItemsList.MenuItems[0].MenuItemQuantities = await _ownerService.GetQuantityById(menuItemId);
                List<Quantity> quantity = await _commonService.GetAllQuantity();
                var allQuantities = quantity
                .Select(q => new Quantity
                {
                    Id = q.Id,
                    Size = q.Size,
                    IsSelected = menuCategoryItemsList.MenuItems[0].MenuItemQuantities.Any(p => p.QuantityId == q.Id) // mark as selected
                }).ToList();
                menuCategoryItemsList.Quantities = allQuantities;
                menuCategoryItemsList.MenuItems[0].MenuCategories = await _ownerService.GetCategoriesByMenuId(menuItemId);
                List<FoodType> foodTypes = await _commonService.GetAllFoodType();
                menuCategoryItemsList.FoodTypes = foodTypes;
                return View("AddEditMenu", menuCategoryItemsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while showing AddEditMenu.");
                return View("Error");
            }
        }

        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ShowMenuCategory(int menuCategoryId)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                MenuCategoryViewModel menuCategory = new MenuCategoryViewModel();
                if (menuCategoryId != -1)
                    menuCategory.MenuCategoryDetails = await _ownerService.GetMenuCategoryById(menuCategoryId);
                else
                    menuCategory.MenuCategoryDetails = new MenuCategory();

                menuCategory.MenuItems = await _ownerService.GetAllMenuItemsStored();

                return View("AddEditCategory", menuCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while showing AddEditCategory.");
                return View("Error");
            }
        }

        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> DeleteCategory(int menuCategoryId)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var result = await _ownerService.DeleteCategoryById(menuCategoryId);

                if(result)
                {
                    TempData["SuccessMessage"] = $"The schedule has been removed.";
                }

                var allMenus = await _ownerService.GetAllMenuCategoriesStored();
                if (allMenus == null)
                {
                    _logger.LogInformation($"There are no category.");
                    TempData["ErrorMessage"] = $"There are no category.";
                    return RedirectToAction("ManageCategories");
                }

                return View("ManageCategories", allMenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while showing AddEditCategory.");
                return View("Error");
            }
        }

        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> DeleteMenuItem(int menuItemId)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var result = await _ownerService.DeleteMenuItemById(menuItemId);

                if (result)
                {
                    TempData["SuccessMessage"] = $"The menu item has been removed.";
                }

                var allMenus = await _ownerService.GetAllMenuItemsStored();
                if (allMenus == null)
                {
                    _logger.LogInformation($"There are no menus.");
                    TempData["ErrorMessage"] = $"There are no menus.";
                    return RedirectToAction("ManageMenus");
                }
                return View("ManageMenus", allMenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while updating order.");
                return View("Error");
            }
        }

        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> AddEditMenu(MenuCategoryItemsList model, string? actionType, string selectedCategoryIds, int selectedFoodTypeId, IFormFile ImageFile)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "images");

                    // Ensure the folder exists
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    model.MenuItems[0].ImageUrl = "/images/" + fileName; 
                }

                var selectedQuantityIds = model.Quantities
                    .Where(c => c.IsSelected)
                    .Select(c => c.Id)
                    .ToList();
                List<int> categoryList = selectedCategoryIds
                   .Split(',')
                   .Select(int.Parse)
                   .ToList();
                AddEditMenuItemAsync(model.MenuItems[0], selectedQuantityIds, categoryList, selectedFoodTypeId);
                return RedirectToAction("ManageMenus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while showing AddEditMenu.");
                return View("Error");
            }
        }

        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> AddEditCategory(MenuCategoryViewModel model, string? actionType, string selectedCategoryIds)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }
                List<int> menuItemList = selectedCategoryIds
                   .Split(',')
                   .Select(int.Parse)
                   .ToList();
                AddEditCategoryDetails(model.MenuCategoryDetails, menuItemList);
                return RedirectToAction("ManageCategories");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while showing AddEditCategory.");
                return View("Error");
            }
        }

        // Logs out the Restaurant Owner, clearing their authentication cookie.
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception during Logout.");
                ViewBag.ErrorMessage = "An error occurred. Please try again later.";
                return RedirectToAction("Login");
            }
        }

        public async Task<IActionResult> AddEditMenuItemAsync(MenuItem model, List<int> selectedQuantityIds, List<int> selectedCategoryIds, int selectedFoodTypeId)
        {
            int menuItemID = model?.MenuItemId ?? 0;
            try
            {
                // Retrieve the current user's ID from claims.
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                if (model.ImageUrl.IsNullOrEmpty())
                {
                    string defaultImageUrl = _configuration.GetValue<string>("DefaultSetting:ImageURL") ?? "";
                    model.ImageUrl = defaultImageUrl;
                }

                // Update the address via the service.
                bool updateResult = await _ownerService.AddEditMenuItemAsync(model, menuItemID, selectedQuantityIds, selectedCategoryIds, selectedFoodTypeId);
                if (updateResult)
                {
                    TempData["SuccessMessage"] = "Your menu details have been added/updated successfully.";
                    return RedirectToAction("ManageMenus");
                }
                ModelState.AddModelError("", "Failed to add/update menu. Please try again.");
                //return View(model);
                return RedirectToAction("ManageMenus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during menu add/update. Menu ID: {menuItemID}", menuItemID);
                ModelState.AddModelError("", "An error occured while adding/updating your menu. Please try again later.");
                //return View(model);
                return RedirectToAction("ManageMenus");
            }
        }
        public async Task<IActionResult> AddEditCategoryDetails(MenuCategory model, List<int> selectedCategoryIds)
        {
            int categoryID = model?.MenuCategoryId ?? 0;
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                bool updateResult = await _ownerService.AddEditCategoryAsync(model, categoryID, selectedCategoryIds);
                if (updateResult)
                {
                    TempData["SuccessMessage"] = "Your schedule details have been added/updated successfully.";
                    return RedirectToAction("ManageCategories");
                }
                ModelState.AddModelError("", "Failed to add/update schedule. Please try again.");
                return RedirectToAction("ManageCategories");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during schedule add/update. Category ID: {categoryID}", categoryID);
                ModelState.AddModelError("", "An error occured while adding/updating your schedule. Please try again later.");
                return RedirectToAction("ManageCategories");
            }
        }
        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> ShowPickupTime(int pickupTimeId)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                PickUpTime pickupTime = new PickUpTime();
                if (pickupTimeId != -1)
                    pickupTime = await _ownerService.GetPickupTimeById(pickupTimeId);

                return View("AddEditPickupTime", pickupTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while showing AddEditPickupTime.");
                return View("Error");
            }
        }

        [Authorize(Roles = "RestaurantOwner")]
        public async Task<IActionResult> DeletePickupTime(int id)
        {
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var result = await _ownerService.DeletePickupTimeById(id);

                if (result)
                {
                    TempData["SuccessMessage"] = $"The pickup time has been removed.";
                }

                return RedirectToAction("ManagePickupTime");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while showing DeletePickupTime.");
                return View("Error");
            }
        }
        public async Task<IActionResult> AddEditPickupTime(PickUpTime model)
        {
            int id = model?.Id ?? 0;
            try
            {
                var userIdStr = User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return RedirectToAction("Login");
                }

                bool updateResult = await _ownerService.AddEditPickupTimeAsync(model, id);
                if (updateResult)
                {
                    TempData["SuccessMessage"] = "Your pickup time details have been added/updated successfully.";
                    return RedirectToAction("ManagePickupTime");
                }
                ModelState.AddModelError("", "Failed to add/update pickup time. Please try again.");
                return RedirectToAction("ManagePickupTime");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during pickup time add/update. ID: {id}", id);
                ModelState.AddModelError("", "An error occured while adding/updating your pickup time. Please try again later.");
                return RedirectToAction("ManagePickupTime");
            }
        }
    }

}