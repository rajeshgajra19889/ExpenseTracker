using Asp.Versioning;
using CsvHelper;
using CsvHelper.Configuration;
using ExpenseTracker.Data;
using ExpenseTracker.DTOs;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace ExpenseTracker.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    // [Route("api/[controller]")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]

    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }
        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /api/expenses?categoryId=2&from=2026-01-01&to=2026-08-01
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses(
            [FromQuery] int? categoryId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? sortBy = "date",
            [FromQuery] string? sortDir = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            //var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var query = _context.Expenses.Include(e => e.Category).AsQueryable();
            query = query.Where(e => e.UserId == GetUserId());

            if (categoryId.HasValue)
                query = query.Where(e => e.CategoryId == categoryId);

            if (from.HasValue)
                query = query.Where(e => e.Date >= from);

            if (to.HasValue)
                query = query.Where(e => e.Date <= to);

            query = (sortBy?.ToLower(), sortDir?.ToLower()) switch
            {
                ("name", "asc") => query.OrderBy(b => b.Amount),
                ("amount", "desc") => query.OrderByDescending(b => b.Amount),
                ("category", "asc") => query.OrderBy(b => b.Category.Name),
                ("category", "desc") => query.OrderByDescending(b => b.Category.Name),
                ("date", "asc") => query.OrderBy(b => b.Date),
                _ => query.OrderByDescending(b => b.Date)// default: date desc

            };


            var totalCount = await query.CountAsync();

            var expenses = await query
                .OrderByDescending(e => e.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExpenseDto
                {
                    Id = e.Id,
                    Amount = e.Amount,
                    Date = e.Date,
                    Description = e.Description,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category.Name,
                    ReceiptUrl = e.ReceiptUrl
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCOunt = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Data = expenses
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ExpenseDto>> GetExpense(int id)
        {
            var expense = await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (expense == null) return NotFound();

            return Ok(new ExpenseDto
            {
                Id = expense.Id,
                Amount = expense.Amount,
                Date = expense.Date,
                Description = expense.Description,
                CategoryId = expense.CategoryId,
                CategoryName = expense.Category.Name,
                ReceiptUrl=expense.ReceiptUrl
            });
        }

        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> CreateExpense(CreateExpenseDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);


            if (!categoryExists) return BadRequest("Invalid CategoryId.");

            var expense = new Expense
            {
                Amount = dto.Amount,
                Date = dto.Date,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                UserId = userId // TODO: replace with logged-in user's Id once JWT auth is added
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, UpdateExpenseDto dto)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();

            expense.Amount = dto.Amount;
            expense.Date = dto.Date;
            expense.Description = dto.Description;
            expense.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();

            // _context.Expenses.Remove(expense);
            expense.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpGet("export")]
        public async Task<IActionResult> ExportExpenses(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var query = _context.Expenses
                .Include(x => x.Category)
                .Where(x => x.UserId == userId)
                .AsQueryable();
            if (from.HasValue)
                query = query.Where(x => x.Date >= from);
            if (to.HasValue)
                query = query.Where(x => x.Date <= to);
            var expenses = await query
                .OrderByDescending(x => x.Date)
                .Select(x => new
                {
                    x.Date,
                    Category = x.Category.Name,
                    x.Amount,
                    x.Description
                })
                .ToListAsync();


            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.WriteRecords(expenses);
            writer.Flush();
            memoryStream.Position = 0;

            var fileName = $"expenses_{DateTime.UtcNow:yyyyMMdd}.csv";
            return File(memoryStream.ToArray(), "text/csv", fileName);
        }

        [HttpPost("{id}/receipt")]
        public async Task<ActionResult> UploadReceipt(int Id, IFormFile file)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var expense = await _context.Expenses.FirstOrDefaultAsync(x => x.Id == Id && x.UserId == userId);
            if (expense == null) return NotFound();
            if (file == null || file.Length == 0)
                return BadRequest("No File Upload");
            var allowedExtensions = new[] { ".jpg", ".png", ".jpeg", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Only JPG, PNG or PDF files are allowed.");
            if (file.Length > 5 * 1024 * 1024)//5 MB Limit
                return BadRequest("File too large. Max size is 5MB.");
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "receipts");
            Directory.CreateDirectory(uploadPath);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            expense.ReceiptUrl = $"/receipts/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { receiptUrl = expense.ReceiptUrl });

        }
    }
}
