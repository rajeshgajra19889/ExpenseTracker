using ExpenseTracker.Data;
using ExpenseTracker.DTOs;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/expenses?categoryId=2&from=2026-01-01&to=2026-08-01
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses(
            [FromQuery] int? categoryId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
         
            var query = _context.Expenses.Include(e => e.Category).AsQueryable();
            query = query.Where(e => e.UserId == userId);
          
            if (categoryId.HasValue)
                query = query.Where(e => e.CategoryId == categoryId);

            if (from.HasValue)
                query = query.Where(e => e.Date >= from);

            if (to.HasValue)
                query = query.Where(e => e.Date <= to);

            var expenses = await query
                .OrderByDescending(e => e.Date)
                .Select(e => new ExpenseDto
                {
                    Id = e.Id,
                    Amount = e.Amount,
                    Date = e.Date,
                    Description = e.Description,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category.Name
                })
                .ToListAsync();

            return Ok(expenses);
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
                CategoryName = expense.Category.Name
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

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
