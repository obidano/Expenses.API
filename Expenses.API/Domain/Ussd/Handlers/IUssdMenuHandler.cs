using Expenses.API.Domain.Ussd.Models;

namespace Expenses.API.Domain.Ussd.Handlers {
    public interface IUssdMenuHandler {
        Task<UssdHandlerResult> HandleAsync(UssdRequest request, UssdState state);
    }
}
