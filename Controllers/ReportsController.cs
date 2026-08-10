using Asp.Versioning;
using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Security.Claims;

namespace ExpenseTracker.Controllers
{
    [ApiVersion("1.0")]
  //  [Route("api/[controller]")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]

    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [OutputCache(Duration = 30)]
        // GET /api/reports/by-category?year=2026&month=8
        [HttpGet("by-category")]
        public async Task<IActionResult> GetSpendByCategory([FromQuery] int? year, [FromQuery] int? month)
        {
            var query = _context.Expenses.AsQueryable();

            if (year.HasValue)
                query = query.Where(e => e.Date.Year == year);

            if (month.HasValue)
                query = query.Where(e => e.Date.Month == month);

            var result = await query
                .GroupBy(e => e.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(e => e.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            return Ok(result);
        }

        // GET /api/reports/monthly-trend?year=2026
        [HttpGet("monthly-trend")]
        public async Task<IActionResult> GetMonthlyTrend([FromQuery] int? year)
        {
            year ??= DateTime.UtcNow.Year;

            var result = await _context.Expenses
                .Where(e => e.Date.Year == year)
                .GroupBy(e => e.Date.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(e => e.Amount)
                })
                .OrderBy(g => g.Month)
                .ToListAsync();

            return Ok(result);
        }

        // GET /api/reports/top-categories?count=5
        [HttpGet("top-categories")]
        public async Task<IActionResult> GetTopCategories([FromQuery] int count = 5)
        {
            var result = await _context.Expenses
                .GroupBy(e => e.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(e => e.Amount)
                })
                .OrderByDescending(g => g.Total)
                .Take(count)
                .ToListAsync();

            return Ok(result);
        }

        // GET /api/reports/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalSpend = await _context.Expenses.SumAsync(e => e.Amount);
            var totalCount = await _context.Expenses.CountAsync();
            var avgExpense = totalCount > 0 ? totalSpend / totalCount : 0;

            var thisMonth = await _context.Expenses
                .Where(e => e.Date.Month == DateTime.UtcNow.Month && e.Date.Year == DateTime.UtcNow.Year)
                .SumAsync(e => e.Amount);

            return Ok(new
            {
                TotalSpend = totalSpend,
                TotalExpenses = totalCount,
                AverageExpense = Math.Round(avgExpense, 2),
                CurrentMonthSpend = thisMonth
            });
        }

        [Authorize]
        [HttpGet("budget-status")]
        public async Task<IActionResult> GetBudgetStatus([FromQuery] int month, [FromQuery] int year)
        {
            var userID = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var budgets = await _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.UserId == userID && b.Month == month && b.Year == year)
                .ToListAsync();
            var actualSpend = await _context.Expenses
                .Where(e => e.UserId == userID && e.Date.Month == month && e.Date.Year == year)
                .GroupBy(e => e.CategoryId)
                .Select(g => new { CategoryId = g.Key, Total = g.Sum(e => e.Amount) })
                .ToListAsync();
            var result=budgets.Select(b=>new
            {

                Category=b.Category.Name,
                Limit=b.MonthlyLimit,
                Spent = actualSpend.FirstOrDefault(a => a.CategoryId == b.CategoryId)?.Total ?? 0,
                Remaining = b.MonthlyLimit - (actualSpend.FirstOrDefault(a => a.CategoryId == b.CategoryId)?.Total ?? 0),
                OverBudget = (actualSpend.FirstOrDefault(a => a.CategoryId == b.CategoryId)?.Total ?? 0) > b.MonthlyLimit
            });

            return Ok(result);


        }
    }
}
