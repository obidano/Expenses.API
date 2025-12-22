using Expenses.API.Domain.Ussd.Models;

namespace Expenses.API.Domain.Ussd.Services {
    public abstract class UssdStateService {
        public abstract Task<UssdState?> GetStateAsync(string phoneNumber);
        public abstract Task SaveStateAsync(string phoneNumber, UssdState state);
        public abstract Task ClearStateAsync(string phoneNumber);
    }
}
