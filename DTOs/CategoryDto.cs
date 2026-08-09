using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 30 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
