using Expenses.API.Domain.Transaction;
using Expenses.API.Domain.Transaction.Dto;
using Expenses.API.framework.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Expenses.API.Domain.Transaction.Services {
    public class TransactionServiceImpl(AppDbContext context) : TransactionService {
        public override async Task<Transaction> createTransaction(CreateTransaction payload) {
            var transaction = new Transaction {
                Amount = payload.Amount,
                Category = payload.Category,
                Description = payload.Description,
                Type = payload.Type,
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow

            };
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();
            return transaction;
        }

        public override async Task<List<Transaction>> getAllTransactions() {
            return await context.Transactions.AsNoTracking().ToListAsync();
        }

        public override async Task<(List<Transaction> Data, int TotalCount)> getAllTransactions(TransactionFilters filters) {
            // Build query with AsNoTracking for better performance (read-only)
            var query = context.Transactions.AsNoTracking();
            
            // Apply filters
            if (!string.IsNullOrEmpty(filters.type)) {
                query = query.Where(t => t.Type == filters.type);
            }
            if (!string.IsNullOrEmpty(filters.category)) {
                query = query.Where(t => t.Category == filters.category);
            }

            var totalCount = await query.CountAsync();

            
            // Apply sorting
            query = filters.SortBy?.ToLower() switch {
                "amount" => filters.SortDescending 
                    ? query.OrderByDescending(t => t.Amount) 
                    : query.OrderBy(t => t.Amount),
                "description" => filters.SortDescending 
                    ? query.OrderByDescending(t => t.Description) 
                    : query.OrderBy(t => t.Description),
                "category" => filters.SortDescending 
                    ? query.OrderByDescending(t => t.Category) 
                    : query.OrderBy(t => t.Category),
                _ => filters.SortDescending 
                    ? query.OrderByDescending(t => t.CreatedAt) 
                    : query.OrderBy(t => t.CreatedAt)
            };
            
            // Calculate skip and apply pagination
            var skip = (filters.Page - 1) * filters.PageSize;
            
            // Get count and data - both use the same filtered+sorted query
            var data = await query.Skip(skip).Take(filters.PageSize).ToListAsync();
            
            return (data, totalCount);
        }

        public override async Task<Transaction?> getTransactionById(string id) {
            var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == id);
            return transaction;
        }

        public override async Task<Transaction?> getTransactionById(TransactionFilters filters) {
            var query = context.Transactions.AsNoTracking();
            if (!string.IsNullOrEmpty(filters.id)) {
                query = query.Where(t => t.Id == filters.id);
            }
            return await query.FirstOrDefaultAsync();
        }

        public override async Task<Transaction?> updateTransaction(string id, UpdateTransaction payload) {
            var query = context.Transactions.AsQueryable();
            query = query.Where(t => t.Id == id);
            var transaction = await query.FirstOrDefaultAsync();
            if (transaction == null) {
                return null;
            }

            transaction.Type = payload.Type;
            transaction.Category = payload.Category;
            transaction.Description = payload.Description ?? "";
            transaction.UpdatedAt = DateTime.UtcNow;

            context.Transactions.Update(transaction);
            await context.SaveChangesAsync(); 
            return transaction;
        }

        public override async Task<bool?> deleteTransaction(string id) {
            // Use FindAsync directly - more efficient and always tracked
            var transaction = await context.Transactions.FindAsync(id);
            if (transaction == null) return null;
            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();
            return true;
        }

        public override async Task<BalanceResult> calculateBalance(TransactionFilters? filters = null) {
            // Build query with AsNoTracking for better performance (read-only)
            var query = context.Transactions.AsNoTracking();
            
            // Apply filters if provided
            if (filters != null) {
                if (!string.IsNullOrEmpty(filters.type)) {
                    query = query.Where(t => t.Type == filters.type);
                }
                if (!string.IsNullOrEmpty(filters.category)) {
                    query = query.Where(t => t.Category == filters.category);
                }
            }

            // Calculate totals and count
            var totalIncome = await query
                .Where(t => t.Type == "Income")
                .SumAsync(t => t.Amount);
            
            var totalExpense = await query
                .Where(t => t.Type == "Expense")
                .SumAsync(t => t.Amount);
            
            var transactionCount = await query.CountAsync();
            
            var balance = totalIncome - totalExpense;

            return new BalanceResult {
                Balance = balance,
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                TransactionCount = transactionCount
            };
        }
    }
}
