# Training-Agency (ASP.NET Core MVC)

## Overview
An ASP.NET Core MVC application using:
- ASP.NET Core Identity (authentication/authorization)
- EF Core + SQL Server (data access)
- Repository pattern (DI-registered repositories)
- Options pattern (strongly-typed admin configuration for seeding)
- Serilog + a request timing middleware (logging)

## Setup
### Prerequisites
- .NET 8 SDK
- SQL Server instance (Windows auth or matching connection string)

### Configure `appsettings.json`
Update:
- `ConnectionStrings:DB`
- `Admin:Email`, `Admin:Password`, `Admin:Name`, `Admin:Address`

### Apply migrations
```powershell
dotnet ef database update
```

### Run
```powershell
dotnet run
```

On startup, the app seeds:
- Identity roles (`Admin`, `Instructor`, `Student`)
- A default admin user from `AdminOptions` (if not present)

## Architecture Notes
### Repository pattern
Contracts live under `Repositories/`:
- `IRepository<T>`: `CreateAsync`, `ReadAll`, `Read`, `Update`, `Delete`
- Specialized interfaces: `ICourseRepository`, `IDepartmentRepository`, `IInstructorRepository`, `ITraineeRepository`

Implementations are EF Core-based and registered in `Program.cs` with `AddScoped`.

### Options pattern (default admin seeding)
`MVC.Options.AdminOptions` is bound from the `Admin` section in `appsettings.json` via:
- `builder.Services.AddOptions<AdminOptions>().Bind(...).ValidateOnStart()`

`Services/DataSeeder.cs` consumes `IOptions<AdminOptions>` to seed roles and the default admin.

### Logging
- Serilog is configured in `Program.cs` (console + daily rolling file under `logs/`)
- `Middlewares/RequestTimingMiddleware` logs slower requests (> `1000 ms`) as `Warning`

## Main Endpoints (high level)
- `AccountController`: Login/Register/Profile/Edit/Logout
- `InstructorController` (Admin-only): CRUD + helper to get courses by department (JSON)
- `DepartmentController` (Admin-only): CRUD + details
- `CourseController` (Admin-only): CRUD + AJAX validation helper (`lessThanDegree`)

## Known Issues / Notes
- `Controllers/TraineeController.cs` is currently empty, but `Views/Trainee/*` exist.
- Some role naming references use `Trainee`, while role seeding creates `Student`. Align these if you see auth/navigation issues.
- `Models/DBContext.cs` contains a hardcoded SQL connection string in `OnConfiguring`; `Program.cs` also configures `Db` via `ConnectionStrings:DB`.

# Training-Agency (ASP.NET Core MVC)

This project is an ASP.NET Core MVC application that uses:
- ASP.NET Core Identity for authentication/authorization
- Entity Framework Core + SQL Server for data access
- A repository pattern (interfaces + EF implementations registered in DI)
- The Options pattern for strongly-typed configuration (default admin seeding)
- Custom middleware for request timing logging

---

## Project Structure

Key folders in this repository:
- `Controllers/`: MVC controller endpoints (`Account`, `Course`, `Department`, `Instructor`, and an empty `TraineeController`)
- `Repositories/`: repository contracts + EF Core implementations (`IRepository<T>` + entity-specific repositories)
- `Options/`: strongly-typed configuration objects (`AdminOptions`)
- `Services/`: startup services (startup seeding via `DataSeeder`)
- `Middlewares/`: custom ASP.NET Core middleware (`RequestTimingMiddleware`)
- `Models/`: EF Core entities + Identity user (`DBContext`, `Department`, `Course`, `Instructor`, `Trainee`, etc.)
- `ModelView/`: view-models used by Razor views and controller actions (for example `RegisterMV`, `LoginMV`, `UserMV`, `TraineeDegreeCourseMV`)
- `Views/`: Razor UI (including `Views/Trainee/*` and `Views/Shared/_NavPartial.cshtml`)
- `Migrations/`: EF Core migrations snapshot for the `Db` context
- `logs/`: Serilog file sink output (rolling daily)

---

## Architecture Overview

### Repository pattern (DI + EF Core)
The project defines a generic repository interface:
`Repositories/IRepository.cs`
- `Task CreateAsync(T c)`
- `List<T> ReadAll()`
- `T? Read(int id)`
- `void Update(T newObject, int id)`
- `void Delete(int id)`

Then each entity adds specialized queries via derived interfaces:
- `ICourseRepository` (extra reads like `ReadAllWithDepartments`, `GetCoursesForDepartment`, etc.)
- `IDepartmentRepository` (extra read like `ReadWithCourses`)
- `IInstructorRepository` (extra reads like `ReadWithDepartment`, `ReadByEmail`)
- `ITraineeRepository` (extra reads like `ReadTraineeWithResults`)

Concrete implementations are EF Core-based and are registered in `Program.cs` as `Scoped`:
- `IDepartmentRepository -> DepartmentRepository`
- `ICourseRepository -> CourseRepository`
- `ITraineeRepository -> TraineeRepository`
- `IInstructorRepository -> InstructorRepository`

### Options pattern (strongly-typed configuration)
`Options/AdminOptions.cs` defines `AdminOptions` with:
- `Email`
- `Password`
- `Name`
- `Address`

`Program.cs` binds and validates this options section:
- `builder.Services.AddOptions<AdminOptions>()`
- `.Bind(builder.Configuration.GetSection(AdminOptions.SectionName))`
- `.Validate(...)` for:
  - email contains `@`
  - password includes at least one upper, one lower, and one digit
- `.ValidateOnStart()`

`Services/DataSeeder.cs` receives `IOptions<AdminOptions>` and uses it to:
- create identity roles (startup seed)
- create a default admin user (startup seed) if it does not exist

### Logging + middleware
- Serilog is configured in `Program.cs`:
  - reads from configuration
  - logs to console
  - logs to file: `logs/app-.log` (rolling daily)
- `Middlewares/RequestTimingMiddleware.cs` measures each request duration:
  - logs `Information` normally
  - logs `Warning` for requests taking more than `1000 ms`

---

## Startup / Request Pipeline (Program.cs)

Key parts of the pipeline:
1. Database/Identity setup:
   - `AddDbContext<Db>` using connection string from `appsettings.json` (`ConnectionStrings:DB`)
   - `AddIdentity<ApplicationUser, IdentityRole<int>>` with EF stores
2. Repository DI:
   - repositories registered as scoped services
3. Session:
   - `builder.Services.AddSession()`
4. Serilog:
   - `builder.Host.UseSerilog(...)`
5. Seed on startup:
   - `DataSeeder.SeedAsync()` runs inside a `CreateScope()` before the app starts serving requests
6. Middleware order:
   - `UseExceptionHandler` + `UseHsts` (non-development)
   - `UseHttpsRedirection`
   - `UseMiddleware<RequestTimingMiddleware>`
   - `UseStaticFiles`
   - `UseRouting`
   - `UseSession`
   - `UseAuthentication`
   - `UseAuthorization`
7. Default route:
   - `{controller=Account}/{action=Login}/{id?}`

---

## Authentication & Authorization

### Roles
`DataSeeder.SeedRolesAsync()` creates these roles:
- `Admin`
- `Instructor`
- `Student`

### Note: role naming mismatch to be aware of
There are places in the code that reference `Trainee` (for example `AccountController.index` checks `User.IsInRole("Trainee")`).
`AccountController.Register` also adds new users to role `Trainee`.
`DataSeeder` does not seed a `Trainee` role (it seeds `Student`).

If you hit role-related issues during registration/authorization, align the role names (for example, seed `Trainee` instead of `Student`, or update the references so they match).

---

## Configuration

### `appsettings.json`
The required keys (used by the app) are:
- `ConnectionStrings:DB`
  - used by `AddDbContext<Db>` in `Program.cs`
- `Admin` section (bound to `AdminOptions`)
  - `Email`
  - `Password`
  - `Name`
  - `Address`

Example (from current repo):
- `ConnectionStrings:DB`
  - `Server=ABOHEND;Database=TrainingAgency;Trusted_Connection=True;Encrypt=False`
- `Admin:Email`
  - `admin@agency.com`

### `appsettings.Development.json`
Current contents only configure basic logging and do not override the connection string or admin options.

---

## Database / EF Core

### DbContext
`Models/DBContext.cs` defines DbSets:
- `Departments`
- `Instructors`
- `Trainees`
- `Courses`
- `CoursesResults`

It also contains `OnConfiguring` that hardcodes a SQL Server string:
`Server=.;Database=MVC;Trusted_Connection=True;Encrypt=False`

However, `Program.cs` configures `Db` via `AddDbContext<Db>(...)` using the `ConnectionStrings:DB` value.
Depending on EF Core options behavior, the hardcoded `OnConfiguring` may or may not be used.

If you see unexpected database connection behavior, update/remove the hardcoded `OnConfiguring` connection string so the app consistently uses `appsettings.json`.

### Migrations
The repo contains a `Migrations/` folder.
To apply migrations:
- ensure the `ConnectionStrings:DB` value points to the target SQL Server database
- run EF migrations update (see next section)

---

## How to Run

From the project folder (where `MVC.csproj`/`Program.cs` live):
1. Restore & build:
   - `dotnet restore`
   - `dotnet build`
2. (Recommended) Apply migrations:
   - `dotnet ef database update`
3. Run:
   - `dotnet run`

On startup, the app will seed:
- identity roles (see the role note above)
- the default admin user from `AdminOptions` (if admin does not exist)

---

## Controllers (behavior summary)

### `AccountController`
- `GET /Account/Login`
- `POST /Account/Login` (validates credentials; uses `ModelView/LoginMV`)
- `POST /Account/Register` (creates a `Trainee` and assigns role; uses `ModelView/RegisterMV`)
- profile actions:
  - `GET /Account/index` (shows profile based on role; uses `ModelView/UserMV`)
  - `GET/POST /Account/Edit`
- `GET /Account/Logout`

### `InstructorController` (Admin-only)
- `GET /Instructor/Index` (lists instructors)
- `GET /Instructor/GetOne/{id}`
- `GET/POST /Instructor/Add`
- `GET/POST /Instructor/Edit`
- `GET /Instructor/Delete/{id}`
- `GET /Instructor/GetCoursesForDepartment/{id}` returns JSON courses for a department

### `DepartmentController` (Admin-only)
- `Index`, `Details`
- `Create`, `Edit`, `Delete`

### `CourseController` (Admin-only)
- `GET /Course/Index` lists courses with departments
- `GET/POST /Course/Add`, `GET/POST /Course/Edit`, `GET /Course/Delete/{id}`
- AJAX validation helper:
  - `lessThanDegree(Min_Degree, Degree)` returns JSON `true/false`

### `TestController`
- Session/cookie demonstration endpoints (`Set`, `Get`)

### `TraineeController`
- currently an empty controller file (`Controllers/TraineeController.cs` is empty)
- however, trainee views exist under `Views/Trainee/` and expect actions like:
  - `Index`, `Details`, `Edit`, `Delete`
  - a results endpoint used by `Views/Trainee/GetResults.cshtml`
- `Views/Shared/_NavPartial.cshtml` switches the navbar home/actions when `User.IsInRole("Trainee")` to controller `Trainee`, action `Details`, and result action `GetResults`.
- `Views/Trainee/Edit.cshtml` links back using `asp-action="Results"` (note the mismatch vs `GetResults` in the nav partial).

---

## View Models (`ModelView/`)

View-models defined in this project:
- `ModelView/RegisterMV`
  - `Name`, `Email`, `Password`, `ConfirmPassword`, `Department`
- `ModelView/LoginMV`
  - `Email`, `Password`
- `ModelView/UserMV`
  - profile fields: `Name`, `Address`, `Email`, `PhoneNumber`
  - role-specific fields: `Grade` (for trainee) and `Salary` (for instructor)
- `ModelView/TraineeDegreeCourseMV`
  - `TraineeName`
  - `TraineePoints`: `Dictionary<string, (Decimal?, bool)>` used by `Views/Trainee/GetResults.cshtml`

---

## Repository Pattern: Where to Add New Data Access

To add a new entity with repository support:
1. Create a new interface inheriting `IRepository<T>` (for entity-specific queries).
2. Implement it using EF Core (`Db` injected via constructor).
3. Register it in `Program.cs`:
   - `builder.Services.AddScoped<INewRepository, NewRepository>();`
4. Inject the repository into the appropriate controller constructor and use it instead of direct EF access.

---

## Validation / Unique Course Name

`Models/UniqueNameAttribute` is a `ValidationAttribute` used on `Course.Name`.
It validates that the course name is unique inside the same department.

Implementation note:
- it currently instantiates `Db` directly inside the validator rather than using a repository.

---

## Notes / Known Issues to Revisit
- `DataSeeder` seeds `Student`, but controllers appear to use `Trainee`.
- `DbContext.OnConfiguring` hardcodes a SQL connection string, while `Program.cs` uses `ConnectionStrings:DB`.
- `Controllers/TraineeController.cs` is currently empty.
- `Views/Shared/_NavPartial.cshtml` expects `TraineeController.GetResults`, while `Views/Trainee/Edit.cshtml` links to `TraineeController.Results`.

