namespace Expenses.API.Domain.Ussd.Models {
    public class TransactionHistoryState : UssdState {
        public int TransactionPage { get; set; } = 0;           // Current page (0-based)
        public string? SelectedTransactionId { get; set; }      // Selected transaction for detail view
        
        // Cache all displayed transactions with their display numbers
        // Maps displayNumber -> transactionId for easy selection lookup
        public Dictionary<int, string> DisplayedTransactions { get; set; } = new();
    }
}
