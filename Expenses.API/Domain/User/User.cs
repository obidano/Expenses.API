using Expenses.API.Shared.Models.Base;

namespace Expenses.API.Domain.User {
    public class User: ModelMixin {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }
}
