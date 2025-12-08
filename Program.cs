using BlazorApp1.Components;
using BlazorApp1.Components.Account;
using BlazorApp1.Data;
using BlazorApp1.Services;
using Blazorise;
using Blazorise.Tailwind;
using Blazorise.Icons.FontAwesome;
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

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Register StoreDbContext for custom User and AuditLog entities
var storeConnectionString = builder.Configuration.GetConnectionString("StoreConnection") ?? "Data Source=store.db";
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlite(storeConnectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Register Repository and Services for custom authentication
builder.Services.AddScoped<IRepository<User>, Repository<User>>();
builder.Services.AddScoped<IRepository<AuditLog>, Repository<AuditLog>>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register Cart and Order services for store functionality
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICustomerOrderService, CustomerOrderService>();

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

    // Seed sample products if not exists
    if (!storeContext.Products.Any())
    {
        var electronics = storeContext.Categories.First(c => c.CategoryName == "Electronics");
        var clothing = storeContext.Categories.First(c => c.CategoryName == "Clothing");
        var books = storeContext.Categories.First(c => c.CategoryName == "Books");

        var products = new[]
        {
            new Product { ProductName = "Wireless Headphones", Price = 79.99m, CostPrice = 45.00m, CategoryId = electronics.CategoryId, Description = "High-quality wireless headphones with noise cancellation", Status = "active", Unit = "pcs" },
            new Product { ProductName = "Smart Watch", Price = 199.99m, CostPrice = 120.00m, CategoryId = electronics.CategoryId, Description = "Feature-rich smartwatch with health tracking", Status = "active", Unit = "pcs" },
            new Product { ProductName = "Bluetooth Speaker", Price = 49.99m, CostPrice = 25.00m, CategoryId = electronics.CategoryId, Description = "Portable bluetooth speaker with amazing sound", Status = "active", Unit = "pcs" },
            new Product { ProductName = "Men's T-Shirt", Price = 24.99m, CostPrice = 12.00m, CategoryId = clothing.CategoryId, Description = "Comfortable cotton t-shirt for everyday wear", Status = "active", Unit = "pcs" },
            new Product { ProductName = "Women's Dress", Price = 59.99m, CostPrice = 30.00m, CategoryId = clothing.CategoryId, Description = "Elegant dress for special occasions", Status = "active", Unit = "pcs" },
            new Product { ProductName = "Programming Guide", Price = 39.99m, CostPrice = 20.00m, CategoryId = books.CategoryId, Description = "Complete guide to modern programming", Status = "active", Unit = "pcs" },
            new Product { ProductName = "Novel Collection", Price = 29.99m, CostPrice = 15.00m, CategoryId = books.CategoryId, Description = "Bestselling novel collection", Status = "active", Unit = "pcs" }
        };
        storeContext.Products.AddRange(products);
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
