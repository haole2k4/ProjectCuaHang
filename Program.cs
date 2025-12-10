using BlazorApp1.Components;
using BlazorApp1.Components.Account;
using BlazorApp1.Data;
using BlazorApp1.Services;
using Blazorise;
using Blazorise.Tailwind;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;
using StoreManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Blazorise
builder.Services
    .AddBlazorise(options =>
    {
        options.Immediate = true;
    })
    .AddTailwindProviders()
    .AddFontAwesomeIcons();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Configure Authentication - Use Cookie as default scheme for custom login
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.Name = "EStore.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Register StoreDbContext for custom User and AuditLog entities
var storeConnectionString = builder.Configuration.GetConnectionString("StoreConnection") ?? "Data Source=store.db";
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlite(storeConnectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity for ApplicationUser (optional - for future use)
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Register Repository and Services for custom authentication
builder.Services.AddScoped<IRepository<User>, Repository<User>>();
builder.Services.AddScoped<IRepository<AuditLog>, Repository<AuditLog>>();
builder.Services.AddScoped<IRepository<Product>, Repository<Product>>();
builder.Services.AddScoped<IRepository<Inventory>, Repository<Inventory>>();
builder.Services.AddScoped<IRepository<Category>, Repository<Category>>();
builder.Services.AddScoped<IRepository<Supplier>, Repository<Supplier>>();
builder.Services.AddScoped<IRepository<Warehouse>, Repository<Warehouse>>();
builder.Services.AddScoped<IRepository<Promotion>, Repository<Promotion>>();
builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
builder.Services.AddScoped<IRepository<OrderItem>, Repository<OrderItem>>();
builder.Services.AddScoped<IRepository<Payment>, Repository<Payment>>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register Cart and Order services for store functionality
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICustomerOrderService, CustomerOrderService>();

// Register Product and Category services for admin functionality
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

// Register Statistics service for admin dashboard
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Register HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Ensure StoreDbContext database is created and seed data
using (var scope = app.Services.CreateScope())
{
    var storeContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    storeContext.Database.EnsureCreated();
    
    // Seed default admin user if not exists
    if (!storeContext.Users.Any(u => u.Username == "admin"))
    {
        storeContext.Users.Add(new User
        {
            Username = "admin",
            Password = "admin123",
            FullName = "Administrator",
            Role = "admin",
            Status = "active",
            CreatedAt = DateTime.Now
        });
        storeContext.SaveChanges();
    }

    // Seed sample categories if not exists
    if (!storeContext.Categories.Any())
    {
        var categories = new[]
        {
            new Category { CategoryName = "Electronics", Status = "active" },
            new Category { CategoryName = "Clothing", Status = "active" },
            new Category { CategoryName = "Books", Status = "active" },
            new Category { CategoryName = "Home & Garden", Status = "active" },
            new Category { CategoryName = "Sports", Status = "active" }
        };
        storeContext.Categories.AddRange(categories);
        storeContext.SaveChanges();
    }

    // Seed default warehouse if not exists
    if (!storeContext.Warehouses.Any())
    {
        storeContext.Warehouses.Add(new Warehouse
        {
            WarehouseName = "Main Warehouse",
            Address = "Main Street 123",
            Status = "active"
        });
        storeContext.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add custom logout endpoint with different path to avoid conflict with Identity
app.MapGet("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
