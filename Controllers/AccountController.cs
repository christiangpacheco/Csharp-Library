using System.Security.Claims;
using Library.DTOs.Account;
using Library.Models;
using Library.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterUserDto());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _authService.RegisterAsync(dto);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error ?? "Não foi possível criar a conta.");
            return View(dto);
        }

        var user = await _authService.GetActiveUserAsync(result.Id!.Value);
        if (user is not null)
        {
            await SignInAsync(user);
        }

        TempData["Success"] = "Conta criada. Bem-vindo(a)!";
        return RedirectToAction("Index", "Books");
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginDto { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var user = await _authService.ValidateCredentialsAsync(dto.Email, dto.Password);
        if (user is null)
        {
            // Mensagem única para não revelar se o e-mail existe ou se a conta está inativa.
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(dto);
        }

        await SignInAsync(user);

        // Só redireciona para URLs locais (evita open redirect).
        if (!string.IsNullOrEmpty(dto.ReturnUrl) && Url.IsLocalUrl(dto.ReturnUrl))
        {
            return LocalRedirect(dto.ReturnUrl);
        }

        return RedirectToAction("Index", "Books");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Você saiu da sua conta.";
        return RedirectToAction("Index", "Books");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
