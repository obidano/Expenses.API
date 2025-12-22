namespace Expenses.API.Domain.Transaction {
    public static class TransactionConstants {
        public static readonly List<string> Categories = new() {
            "salary", "food", "rent", "utilities",
            "transportation", "entertainment", "shopping",
            "healthcare", "education", "other"
        };
    }
}
