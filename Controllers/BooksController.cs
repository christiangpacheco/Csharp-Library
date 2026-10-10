using Library.DTOs.Books;
using Library.Services;
using Library.Services.Interfaces;
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

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var model = await _bookService.SearchAsync(search, page, PageSize);
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await _bookService.GetByIdAsync(id);
        return book is null ? NotFound() : View(book);
    }

    public IActionResult Create()
    {
        return View(new CreateBookDto());
    }

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

    public async Task<IActionResult> Delete(int id)
    {
        var book = await _bookService.GetByIdAsync(id);
        return book is null ? NotFound() : View(book);
    }

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

    private void AddError(ServiceResult result)
    {
        ModelState.AddModelError(result.Field ?? string.Empty, result.Error ?? "Não foi possível salvar.");
    }
}
