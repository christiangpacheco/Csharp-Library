using Library.Data;
using Library.Services;
using Library.Models;
using Library.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<LoanRules>(builder.Configuration.GetSection("LoanRules"));
builder.Services.Configure<PasswordResetOptions>(builder.Configuration.GetSection("PasswordReset"));

builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IBookCopyService, BookCopyService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Em desenvolvimento, o link de redefinição é só registrado no log. A implementação de produção
// (serviço externo com EmailApiKey) entra junto do Key Vault na Leva 8.
builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Seed do primeiro bibliotecário: dados vêm da configuração (User Secrets em dev), nunca do código.
var seed = app.Configuration.GetSection("SeedLibrarian");
if (!string.IsNullOrWhiteSpace(seed["Email"]) && !string.IsNullOrWhiteSpace(seed["Password"]))
{
    using var scope = app.Services.CreateScope();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await authService.EnsureLibrarianAsync(seed["Name"] ?? "Bibliotecário", seed["Email"]!, seed["Password"]!);
}

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Books}/{action=Index}/{id?}");

app.Run();
