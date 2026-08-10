namespace ExpenseTracker.DTOs
{
    public class BudgetDto
    {
        public int Id { get; set; }
        public decimal MonthlyLimit { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CreateBudgetDto
    {
        public decimal MonthlyLimit { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int CategoryId { get; set; }
    }
}
