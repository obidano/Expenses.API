namespace Expenses.API.Domain.Ussd.Models {
    public class UssdRequest {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}
