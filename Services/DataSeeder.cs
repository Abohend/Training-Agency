using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MVC.Models;
using MVC.Options;

namespace MVC.Data
{
    public class DataSeeder
    {
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AdminOptions _adminOptions;

        public DataSeeder(RoleManager<IdentityRole<int>> roleManager,
            UserManager<ApplicationUser> userManager,
            IOptions<AdminOptions> adminOptions)
        {
            this._roleManager = roleManager;
            this._userManager = userManager;
            this._adminOptions = adminOptions.Value;
        }

        public async Task SeedAsync()
        {
            await SeedRolesAsync();
            await SeedAdminAsync();
        }

        public async Task SeedRolesAsync()
        {
            string[] roles = { "Admin", "Instructor", "Student" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole<int>(role));
                }
            }
        }

        public async Task SeedAdminAsync()
        {
            string adminEmail = _adminOptions.Email;
            string adminPassword = _adminOptions.Password;

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Name = _adminOptions.Name,
                    Address = _adminOptions.Address
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}