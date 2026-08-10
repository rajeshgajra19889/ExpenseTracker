using Asp.Versioning;
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
    [ApiVersion("1.0")]
    [Authorize]
    //[Route("api/[controller]")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]

    public class BudgetsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public BudgetsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BudgetDto>>> GetBudgets()
        {

            var UserId = GetUserId();
            var budgest = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == UserId)
            .Select(b => new BudgetDto
            {
                Id = b.Id,
                MonthlyLimit = b.MonthlyLimit,
                Month = b.Month,
                Year = b.Year,
                CategoryName = b.Category.Name

            }).ToListAsync();
            return Ok(budgest);
        }

        [HttpPost]
        public async Task<ActionResult<BudgetDto>> CreateBudget(CreateBudgetDto dto)
        {
            var userId = GetUserId();
            var CategoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!CategoryExists) return BadRequest("Invalid CategoryId");
            var alreadyExists = await _context.Budgets.AnyAsync(b =>
            b.UserId == userId &&
            b.CategoryId == dto.CategoryId &&
            b.Month == dto.Month &&
            b.Year == dto.Year);
            if (alreadyExists) return BadRequest("A budget already exists for this category and month.");
            var budget = new Budget
            {
                MonthlyLimit = dto.MonthlyLimit,
                Year = dto.Year,
                Month = dto.Month,
                CategoryId = dto.CategoryId,
                UserId = userId
            };
            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBudgets), budget);

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBudget(int id)
        {
            var userId = GetUserId();
            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (budget == null) return NotFound();

            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}
