namespace DF.PaymentService.API.Helpers;

public static class InternalAuthConstants
{
    public const string TimestampHeader = "X-Internal-Timestamp";
    public const string NonceHeader = "X-Internal-Nonce";
    public const string SignatureHeader = "X-Internal-Signature";
    public const string ApiKeyHeader = "X-Internal-Key";
}
