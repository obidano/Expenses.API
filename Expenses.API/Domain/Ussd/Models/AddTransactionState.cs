namespace Expenses.API.Domain.Ussd.Models {
    public class AddTransactionState : UssdState {
        public string? TransactionType { get; set; }
        public string? TransactionCategory { get; set; }
        public double? TransactionAmount { get; set; }
        public string? TransactionDescription { get; set; }
        public int CategoryPage { get; set; } = 0; // Track current page for category pagination
    }
}
