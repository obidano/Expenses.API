namespace Expenses.API.Domain.Transaction.Dto {
    public class BalanceResult {
        public double Balance { get; set; }
        public double TotalIncome { get; set; }
        public double TotalExpense { get; set; }
        public int TransactionCount { get; set; }
    }
}
