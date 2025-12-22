namespace Expenses.API.Domain.Ussd.Models {
    public class UssdState {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public string CurrentMenu { get; set; } = UssdMenuName.MainMenu.ToString();
        public int CurrentStep { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
