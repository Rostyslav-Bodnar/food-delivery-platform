namespace DF.OrderService.Contracts.Pagination;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public readonly record struct PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static PageRequest From(int? page, int? pageSize) => new(
        Page: Math.Max(1, page ?? 1),
        PageSize: Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));

    public int Skip => (Page - 1) * PageSize;
}
