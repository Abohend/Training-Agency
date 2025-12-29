using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;
using MVC.Options;
using MVC.Repositories;
using Serilog;

namespace MVC
{
    public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			#region Services
			//! Notes
			//! There are 3 types of services:
			/* 1. Built in already loaded in the IOC container. ex IConfiguration
			   2. Built in needed to be loaded in the IOC container. ex AddSession
			   3. Custom services ex AddScoped */
			//! There are 3 ways to load a service:
			/* 1. AddScoped(): create a object for each http request.
			   2. AddTransient(): create an object for each injection.
			   3. AddSingelton(): create a single object for different requests and clients.*/

			// Add services to the container.
			builder.Services.AddDbContext<Db>(optionBuiler =>
				{
					optionBuiler.UseSqlServer(builder.Configuration.GetConnectionString("DB"));
				}
			);
			builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
				{
					options.Password.RequireNonAlphanumeric = false;
					options.User.RequireUniqueEmail = true;
				})
				.AddUserManager<UserManager<ApplicationUser>>()
				.AddEntityFrameworkStores<Db>();
			builder.Services.AddControllersWithViews();
			builder.Services.AddSession(); // default time is 20min from unuse
			builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
			builder.Services.AddScoped<ICourseRepository, CourseRepository>();
			builder.Services.AddScoped<ITraineeRepository, TraineeRepository>();
			builder.Services.AddScoped<IInstructorRepository, InstructorRepository>();
			builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
			//builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
			builder.Services.AddOptions<AdminOptions>()
				.Bind(builder.Configuration.GetSection(AdminOptions.SectionName))
				.Validate(a => a.Email.Contains('@'), "Email is not valid.")
				.Validate(a => a.Password.Any(char.IsUpper) &&
					a.Password.Any(char.IsLower) &&
					a.Password.Any(char.IsDigit), "Password must have at least one lower, one upper, and one char")
				.ValidateOnStart();
			builder.Services.AddScoped<DataSeeder>();
			#endregion

			builder.Host.UseSerilog((context, config) =>
			{
				config.ReadFrom.Configuration(context.Configuration)
				.WriteTo.Console()
				.WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day);
			});
			
			var app = builder.Build();

			// seed data
			using (var scope = app.Services.CreateScope())
			{
				var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
				await seeder.SeedAsync();
			}

			#region middlewares
			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseSession();

			app.UseAuthentication();

			app.UseAuthorization();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Account}/{action=Login}/{id?}");
            #endregion
            
			

            app.Run();
		}
	}
}
