using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LoanService.Application.Abstractions.Idempotency;

namespace LoanService.Infrastructure.Idempotency;

internal sealed class Sha256RequestHasher : IRequestHasher
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    public string ComputeHash<TRequest>(TRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var json = JsonSerializer.Serialize(request, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}