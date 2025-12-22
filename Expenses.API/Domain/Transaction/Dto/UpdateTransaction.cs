namespace Expenses.API.Domain.Transaction.Dto {
    public class UpdateTransaction {
        public required string Type { get; set; } // e.g., "Income" or "Expense"
        public required string Category { get; set; }
        public  string? Description { get; set; }

    }
}
