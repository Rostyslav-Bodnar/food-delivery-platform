namespace DF.MenuService.Domain.Entities;

public class IdempotencyKey
{
    public required string Key { get; set; }
    public required string Method { get; set; }
    public required string Path { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
