namespace Expenses.API.Domain.Transaction {
    public class TransactionFilters {
        public string? id { get; set; }
        public string? type { get; set; }
        public string? category { get; set; }
        
        // Pagination properties
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        // Sorting properties
        public string? SortBy { get; set; } = "CreatedAt"; // Default sort by creation date
        public bool SortDescending { get; set; } = true; // Default: newest first
    }
}
