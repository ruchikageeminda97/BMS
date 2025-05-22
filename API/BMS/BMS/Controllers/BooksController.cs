using BMS.Data;
using BMS.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetBooks([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1)
            {
                return BadRequest(new { message = "Page number must be greater than or equal to 1." });
            }

            if (pageSize < 1)
            {
                return BadRequest(new { message = "Page size must be greater than or equal to 1." });
            }

            try
            {
                var totalBooks = await _context.Books.CountAsync();

                if (totalBooks == 0)
                {
                    return Ok(new
                    {
                        message = "No books found in the database.",
                        data = new List<Book>(),
                        totalBooks = 0,
                        totalPages = 0,
                        currentPage = pageNumber,
                        pageSize = pageSize
                    });
                }

                var totalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);

                var books = await _context.Books
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    message = "Books retrieved successfully.",
                    data = books,
                    totalBooks = totalBooks,
                    totalPages = totalPages,
                    currentPage = pageNumber,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving books.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid book ID. The ID cannot be empty." });
            }

            var book = await _context.Books.FindAsync(id);

            if (book == null)
            {
                return NotFound(new { message = $"Book with ID {id} not found." });
            }

            return Ok(new { message = $"Book with ID {id} retrieved successfully.", data = book });
        }

        [HttpPost]
        public async Task<ActionResult<Book>> CreateBook(Book book)
        {
            if (book == null)
            {
                return BadRequest(new { message = "Book data is required." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid book data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            if (!string.IsNullOrEmpty(book.isbn) && !IsValidIsbn(book.isbn))
            {
                return BadRequest(new { message = "Invalid ISBN. The ISBN must be a valid 10 or 13-digit number with a correct checksum." });
            }

            try
            {
                book.bookId = Guid.NewGuid();
                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBook), new { id = book.bookId }, new { message = "Book added successfully.", data = book });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding the book.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(Guid id, Book book)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid book ID. The ID cannot be empty." });
            }

            if (id != book.bookId)
            {
                return BadRequest(new { message = "Book ID in the URL does not match the ID in the request body." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid book data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            if (!string.IsNullOrEmpty(book.isbn) && !IsValidIsbn(book.isbn))
            {
                return BadRequest(new { message = "Invalid ISBN. The ISBN must be a valid 10 or 13-digit number with a correct checksum." });
            }

            var existingBook = await _context.Books.FindAsync(id);
            if (existingBook == null)
            {
                return NotFound(new { message = $"Book with ID {id} not found." });
            }

            try
            {
                _context.Entry(existingBook).CurrentValues.SetValues(book);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Book with ID {id} updated successfully.", data = book });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { message = $"Concurrency error: Book with ID {id} may have been modified or deleted by another process." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the book.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new { message = "Invalid book ID. The ID cannot be empty." });
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound(new { message = $"Book with ID {id} not found." });
            }

            try
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Book with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the book.", error = ex.Message });
            }
        }

        private bool IsValidIsbn(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return false;
            }

            isbn = new string(isbn.Where(char.IsDigit).ToArray());

            if (isbn.Length != 10 && isbn.Length != 13)
            {
                return false;
            }

            if (isbn.Length == 10)
            {
                return IsValidIsbn10(isbn);
            }

            if (isbn.Length == 13)
            {
                return IsValidIsbn13(isbn);
            }

            return false;
        }

        private bool IsValidIsbn10(string isbn)
        {
            if (isbn.Any(c => !char.IsDigit(c) && c != 'X'))
            {
                return false;
            }

            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                if (!char.IsDigit(isbn[i]))
                {
                    return false;
                }
                sum += (isbn[i] - '0') * (10 - i);
            }

            int checkDigit;
            if (isbn[9] == 'X' || isbn[9] == 'x')
            {
                checkDigit = 10;
            }
            else if (!char.IsDigit(isbn[9]))
            {
                return false;
            }
            else
            {
                checkDigit = isbn[9] - '0';
            }

            sum += checkDigit;
            return (sum % 11) == 0;
        }

        private bool IsValidIsbn13(string isbn)
        {
            if (isbn.Any(c => !char.IsDigit(c)))
            {
                return false;
            }

            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = isbn[i] - '0';
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            int checkDigit = isbn[12] - '0';
            int remainder = sum % 10;
            int expectedCheckDigit = (remainder == 0) ? 0 : 10 - remainder;

            return checkDigit == expectedCheckDigit;
        }
    }
}