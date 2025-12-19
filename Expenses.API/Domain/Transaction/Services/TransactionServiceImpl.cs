using Expenses.API.Domain.Transaction.Dto;
using Expenses.API.framework.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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
            return await context.Transactions.ToListAsync();
        }

        public override async Task<Transaction?> getTransactionById(string id) {
            var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == id);
            return transaction;
        }

        public override async Task<Transaction?> getTransactionById(TransactionFilters filters) {
            var query = context.Transactions.AsQueryable();
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
            transaction.Description = payload.Description;
            transaction.UpdatedAt = DateTime.UtcNow;

            context.Transactions.Update(transaction);
            await context.SaveChangesAsync(); 
            return transaction;
        }

        public override async Task<bool?> deleteTransaction(string id) {
            var transaction = await getTransactionById(id);
            if (transaction == null) return null;
            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
