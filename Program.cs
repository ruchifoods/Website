using FoodDeliveryApp.Data;
using FoodDeliveryApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;
namespace FoodDeliveryApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSession(); // Add session support
            // Register in-memory caching service for caching data in RAM
            builder.Services.AddMemoryCache();
            // Clear the default logging providers.
            builder.Logging.ClearProviders();
            // Configure the host to use Serilog as the logging provider.
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                // Reads configuration settings for Serilog from appsettings.json.
                configuration.ReadFrom.Configuration(context.Configuration);
            });
            // Configure SQL Server DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );
            // Add Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                // Wherever you want to redirect when unauthenticated and unauthorized
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
            });

            // Register Customer Service
            builder.Services.AddScoped<ICustomerService, CustomerService>();

            // Register Email Service
            builder.Services.AddScoped<IEmailService, EmailService>();

            // Register Common Service
            builder.Services.AddScoped<ICommonService, CommonService>();

            // Register Restaurant Owner Service
            builder.Services.AddScoped<IRestaurantOwnerService, RestaurantOwnerService>();

            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession(); // Enable session middleware
            app.UseRouting();
            // Enable Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
            app.Run();
        }
    }
}