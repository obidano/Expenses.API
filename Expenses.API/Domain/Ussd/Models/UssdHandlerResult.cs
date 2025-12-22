namespace Expenses.API.Domain.Ussd.Models {
    public class UssdHandlerResult {
        public UssdResponse Response { get; set; } = null!;
        public UssdState UpdatedState { get; set; } = null!;

        public UssdHandlerResult(UssdResponse response, UssdState updatedState) {
            Response = response;
            UpdatedState = updatedState;
        }
    }
}
