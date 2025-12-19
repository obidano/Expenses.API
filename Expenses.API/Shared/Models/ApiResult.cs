namespace Expenses.API.Shared.Models {
    public record ApiResult<T>(int Count, IEnumerable<T> Data);
}
