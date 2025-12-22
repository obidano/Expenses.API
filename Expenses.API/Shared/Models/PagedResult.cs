using System.Linq;

namespace Expenses.API.Shared.Models {
    public class PagedResult<T> {
        public int TotalCount { get; set; }
        public int PageCount => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < PageCount;
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();

        public PagedResult(IEnumerable<T> data, int totalCount, int pageNumber, int pageSize) {
            Data = data;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
