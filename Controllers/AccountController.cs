using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MVC.Models;
using MVC.ModelView;
using MVC.Repositories;
using System.Security.Claims;

namespace MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDepartmentRepository departmentRepository;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDepartmentRepository departmentRepository)
        {
            _userManager = userManager;
            this.signInManager = signInManager;
            this.departmentRepository = departmentRepository;
        }

        #region Helpers
        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return await _userManager.FindByIdAsync(id);
        }

        private UserMV MapUser(ApplicationUser? user)
        {
            UserMV? userMV = new();
            userMV.Name = user.Name;
            userMV.Address = user.Address;
            userMV.Email = user.Email;
            userMV.PhoneNumber = user.PhoneNumber;
            return userMV;
        }
        #endregion

        #region index "profile"
        [Authorize]
        public async Task<IActionResult> index()
        {
            ApplicationUser? user = await GetCurrentUserAsync();
            if (user == null)
            {
                RedirectToAction(nameof(Register));
            }
            UserMV userMV = MapUser(user);
            if (User.IsInRole("Trainee"))
            {
                Trainee? trainee = user as Trainee;
                userMV.Grade = trainee!.Grade;
            }
            else if (User.IsInRole("Instructor"))
            {
                Instructor? instructor = user as Instructor;
                userMV.Salary = instructor!.Salary;
            }
            return View(userMV);
        }
        #endregion

        #region Edit
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            ApplicationUser? user = await GetCurrentUserAsync();
            UserMV userMV = MapUser(user);
            return View(userMV);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserMV newUser)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser? user = await GetCurrentUserAsync();
                user.Name = newUser.Name;
                user.Address = newUser.Address;
                user.PhoneNumber = newUser.PhoneNumber;
                await _userManager.UpdateAsync(user);
                return RedirectToAction("Index");
            }
            return View(newUser);
        }
        #endregion

        #region Register

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.deps = departmentRepository.GetDepartmentsSLI();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterMV accountVM)
        {
            if (ModelState.IsValid)
            {
                Trainee newTrainee = new Trainee();
                newTrainee.Email = accountVM.Email;
                newTrainee.UserName = accountVM.Email;
                newTrainee.Name = accountVM.Name;
                newTrainee.DepartmentId = accountVM.Department;
                IdentityResult result =  await _userManager.CreateAsync(newTrainee, accountVM.Password);
                if (result.Succeeded)
                {
                    //add rule
                    await _userManager.AddToRoleAsync(newTrainee, "Trainee");
                    // Save cookie
                    await signInManager.SignInAsync(newTrainee, true);
                    return RedirectToAction("index"); 
                }
                else
                {
                    foreach (var errorItem in result.Errors)
                    {
                        ModelState.AddModelError("Summary", errorItem.Description);
                    }
                }
            }
            return View(accountVM);
           
        }

        #endregion

        #region Login
        [HttpGet]
        public IActionResult Login()
        {
            //check if already logged in
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("index");
            }
            return View();
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Login(LoginMV loginVM)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(loginVM.Email);
            if (user != null)
            {
                // Check Password
                bool result = await _userManager.CheckPasswordAsync(user, loginVM.Password);
                if (result)
                {
                    // create cookie
                    await signInManager.SignInAsync(user, true);
                    return RedirectToAction("Index"); 
                }
            }
            // append error to ModelState
            ModelState.AddModelError("Summary", "Invalid Email or Password");
            return View(loginVM);
        }

        #endregion

        #region Logout
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("login");
        }
        #endregion

    }
}
