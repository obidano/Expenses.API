using Expenses.API.Domain.Transaction.Dto;

namespace Expenses.API.Domain.Transaction.Services {
    public abstract class TransactionService {
        public abstract Task<Transaction> createTransaction(CreateTransaction data);
        public abstract Task<Transaction?> updateTransaction(string id, UpdateTransaction data);
        public abstract Task<Transaction?> getTransactionById(string id);
        public abstract Task<Transaction?> getTransactionById(TransactionFilters filters);
        public abstract Task<List<Transaction>> getAllTransactions();
        public abstract Task<bool?> deleteTransaction(string id);
    }
}
