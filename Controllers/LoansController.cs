using System.Security.Claims;
using Library.DTOs.Loans;
using Library.Models;
using Library.Services;
using Library.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[Authorize]
public class LoansController : Controller
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    // Meus empréstimos: qualquer usuário logado vê os seus.
    public async Task<IActionResult> Index()
    {
        var loans = await _loanService.GetUserLoansAsync(CurrentUserId());
        return View(loans);
    }

    // Gestão: todos os empréstimos em aberto, vencidos primeiro.
    [Authorize(Roles = nameof(UserRole.Librarian))]
    public async Task<IActionResult> Open()
    {
        var loans = await _loanService.GetOpenLoansAsync();
        return View(loans);
    }

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Librarian))]
    public async Task<IActionResult> Create(int bookCopyId)
    {
        var copy = await _loanService.GetAvailableCopyAsync(bookCopyId);
        if (copy is null)
        {
            TempData["Error"] = "Este exemplar não está disponível para empréstimo.";
            return RedirectToAction("Index", "Books");
        }

        await PopulateBorrowersAsync();
        ViewBag.Copy = copy;
        return View(new CreateLoanDto { BookCopyId = bookCopyId });
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Librarian))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLoanDto dto)
    {
        var copy = await _loanService.GetAvailableCopyAsync(dto.BookCopyId);
        if (copy is null)
        {
            TempData["Error"] = "Este exemplar não está mais disponível para empréstimo.";
            return RedirectToAction("Index", "Books");
        }

        if (ModelState.IsValid)
        {
            var result = await _loanService.BorrowAsync(dto.BookCopyId, dto.UserId);
            if (result.Succeeded)
            {
                TempData["Success"] = "Empréstimo registrado.";
                return RedirectToAction(nameof(Open));
            }

            ModelState.AddModelError(result.Field ?? string.Empty, result.Error ?? "Não foi possível registrar o empréstimo.");
        }

        await PopulateBorrowersAsync();
        ViewBag.Copy = copy;
        return View(dto);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Librarian))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id)
    {
        var result = await _loanService.ReturnAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Devolução registrada." : result.Error;

        return RedirectToAction(nameof(Open));
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task PopulateBorrowersAsync()
    {
        ViewBag.Borrowers = await _loanService.GetBorrowersAsync();
    }
}
