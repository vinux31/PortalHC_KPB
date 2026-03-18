namespace HcPortal.Helpers
{
    public record PaginationResult(int CurrentPage, int TotalPages, int TotalCount, int Skip, int Take);

    public static class PaginationHelper
    {
        public static PaginationResult Calculate(int totalCount, int page, int pageSize)
        {
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var currentPage = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
            var skip = (currentPage - 1) * pageSize;
            return new PaginationResult(currentPage, totalPages, totalCount, skip, pageSize);
        }
    }
}
