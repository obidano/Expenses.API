namespace Expenses.API.Domain.Transaction.Dto {
    public class UpdateTransaction {
        public string Type { get; set; } // e.g., "Income" or "Expense"
        public string Description { get; set; }

    }
}
