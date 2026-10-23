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
    private readonly IPasswordResetService _passwordResetService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, IPasswordResetService passwordResetService, IEmailSender emailSender, ILogger<AccountController> logger)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
        _emailSender = emailSender;
        _logger = logger;
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
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordDto());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var token = await _passwordResetService.CreateTokenAsync(dto.Email);

        // Só envia o e-mail quando existe conta ativa, mas a resposta é sempre a mesma:
        // assim a tela não revela se o e-mail está cadastrado (anti-enumeração).
        if (token is not null)
        {
            var link = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme)!;
            try
            {
                await _emailSender.SendPasswordResetAsync(dto.Email, link);
            }
            catch (Exception ex)
            {
                // Falha no envio não vira 500: um erro só para e-mails cadastrados revelaria quem tem conta.
                _logger.LogError(ex, "Falha ao enviar o e-mail de redefinição de senha.");
            }
        }

        TempData["Success"] = "Se houver uma conta com esse e-mail, enviamos um link para redefinir a senha.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult ResetPassword(string? token = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Link inválido ou expirado. Peça um novo.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordDto { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _passwordResetService.ResetAsync(dto);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível redefinir a senha.");
            return View(dto);
        }

        TempData["Success"] = "Senha redefinida. Entre com a nova senha.";
        return RedirectToAction(nameof(Login));
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
