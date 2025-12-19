namespace Expenses.API.Shared.Models.Base {
    public class ModelMixin {
        public string Id { get; set; }

        //dates
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

    }
}
