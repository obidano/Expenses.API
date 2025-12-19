namespace Expenses.API.Domain.Transaction.Dto {
    public class CreateTransaction {
        public string Type { get; set; } // e.g., "Income" or "Expense"
        public string Category { get; set; }
        public double Amount { get; set; }
        public string Description { get; set; }

    }
}
