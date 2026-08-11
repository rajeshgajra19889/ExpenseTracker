using ExpenseTracker.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AdminController(AppDbContext context)

        {
            _context = context;
        }

        [HttpGet("app-expenses")]
        public async Task<IActionResult> GetAllExpenses()
        {
            var expenses = await _context.Expenses
                .Include(x => x.Category)
                .Include(x => x.User)
                .Select(x => new
                {
                    x.Id,
                    x.Amount,
                    x.Date,
                    Category = x.Category.Name,
                    User = x.User.Username
                })
                .ToListAsync();
            return Ok(expenses);

        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, u.Username, u.Email, u.Role })
                .ToListAsync();

            return Ok(users);
        }
    }
}
