using Library.DTOs.Books;
using Library.Services;
using Library.Models;
using Library.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

public class BooksController : Controller
{
    private const int PageSize = 10;

    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    public async Task<IActionResult> Index(string? search, bool inactive = false, int page = 1)
    {
        // Livros inativos só aparecem para o bibliotecário.
        var includeInactive = inactive && User.IsInRole(nameof(UserRole.Librarian));
        var model = await _bookService.SearchAsync(search, includeInactive, page, PageSize);
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await _bookService.GetByIdAsync(id);
        if (book is null || (!book.IsActive && !User.IsInRole(nameof(UserRole.Librarian))))
        {
            return NotFound();
        }

        return View(book);
    }

    [Authorize(Roles = nameof(UserRole.Librarian))]
    public IActionResult Create()
    {
        return View(new CreateBookDto());
    }

    [Authorize(Roles = nameof(UserRole.Librarian))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _bookService.CreateAsync(dto);
        if (!result.Succeeded)
        {
            AddError(result);
            return View(dto);
        }

        TempData["Success"] = "Livro cadastrado.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    [Authorize(Roles = nameof(UserRole.Librarian))]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _bookService.GetByIdAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        return View(new UpdateBookDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            PublishedAt = book.PublishedAt
        });
    }

    [Authorize(Roles = nameof(UserRole.Librarian))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateBookDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _bookService.UpdateAsync(dto);
        if (!result.Succeeded)
        {
            AddError(result);
            return View(dto);
        }

        TempData["Success"] = "Livro atualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = nameof(UserRole.Librarian))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Deactivate(int id) => SetActive(id, false, "Livro desativado.");

    [Authorize(Roles = nameof(UserRole.Librarian))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reactivate(int id) => SetActive(id, true, "Livro reativado.");

    [Authorize(Roles = nameof(UserRole.Librarian))]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _bookService.GetByIdAsync(id);
        return book is null ? NotFound() : View(book);
    }

    [Authorize(Roles = nameof(UserRole.Librarian))]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _bookService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Livro excluído.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SetActive(int id, bool isActive, string message)
    {
        var result = await _bookService.SetActiveAsync(id, isActive);
        if (!result.Succeeded)
        {
            return NotFound();
        }

        TempData["Success"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    private void AddError(ServiceResult result)
    {
        ModelState.AddModelError(result.Field ?? string.Empty, result.Error ?? "Não foi possível salvar.");
    }
}
