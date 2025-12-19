using Expenses.API.Domain.Transaction;
using Expenses.API.Domain.User;
using Microsoft.EntityFrameworkCore;

namespace Expenses.API.framework.Data {
    public class AppDbContext: DbContext{

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }
        // Define DbSets for your entities here
        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
    }
}
