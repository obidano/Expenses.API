namespace Expenses.API.Domain.Ussd.Models {
    public class UssdResponse {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "CON"; // CON or END
        
        public static UssdResponse Continue(string message) {
            return new UssdResponse { Message = message, Type = "CON" };
        }
        
        public static UssdResponse End(string message) {
            return new UssdResponse { Message = message, Type = "END" };
        }
    }
}
