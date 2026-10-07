using Microsoft.AspNetCore.Mvc;
using Library.Models;

namespace Library.Controllers;

public class BooksController : Controller
{
    public IActionResult Index()
    {
        var books = new List<Book>
        {
            new Book
            {
                Id = 1,
                Title = "Clean Code",
                Author = "Robert C. Martin",
                ISBN = "9780132350884",
                PublishedAt = new DateOnly(2008, 8, 1)
            }
        };

        return View("/Views/Account/Books/Index.cshtml", books);
    }
}