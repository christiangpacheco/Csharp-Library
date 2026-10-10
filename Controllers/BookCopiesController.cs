using Library.DTOs.BookCopies;
using Library.Services;
using Library.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

// Exemplares são sempre vistos e editados dentro de um livro: toda ação volta para Books/Details.
public class BookCopiesController : Controller
{
    private readonly IBookCopyService _copyService;

    public BookCopiesController(IBookCopyService copyService)
    {
        _copyService = copyService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookCopyDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Informe um código de até 30 caracteres para o exemplar.";
            return BackToBook(dto.BookId);
        }

        var result = await _copyService.CreateAsync(dto);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Exemplar adicionado." : result.Error;

        return BackToBook(dto.BookId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> MarkLost(int id, int bookId) =>
        Run(bookId, () => _copyService.SetLostAsync(id, true), "Exemplar marcado como perdido.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> MarkAvailable(int id, int bookId) =>
        Run(bookId, () => _copyService.SetLostAsync(id, false), "Exemplar marcado como disponível.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Deactivate(int id, int bookId) =>
        Run(bookId, () => _copyService.SetActiveAsync(id, false), "Exemplar desativado.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reactivate(int id, int bookId) =>
        Run(bookId, () => _copyService.SetActiveAsync(id, true), "Exemplar reativado.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(int id, int bookId) =>
        Run(bookId, () => _copyService.DeleteAsync(id), "Exemplar excluído.");

    private async Task<IActionResult> Run(int bookId, Func<Task<ServiceResult>> action, string successMessage)
    {
        var result = await action();
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? successMessage : result.Error;

        return BackToBook(bookId);
    }

    private IActionResult BackToBook(int bookId) =>
        RedirectToAction("Details", "Books", new { id = bookId });
}
