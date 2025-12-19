using Expenses.API.Shared.Models.Base;

namespace Expenses.API.Domain.Transaction {
    public class Transaction: ModelMixin {
        public string Type { get; set; } // e.g., "Income" or "Expense"
        public string Category { get; set; }
        public double Amount { get; set; }
        public string Description { get; set; }

        //public User User { get; set; }

      
    }
}
