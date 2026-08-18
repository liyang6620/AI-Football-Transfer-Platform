using System.Security.Cryptography;
using System.Text;

namespace FootballTransfer.Api.Middleware;

public sealed class AdminApiKeyMiddleware
{
    private const string HeaderName = "X-Admin-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public AdminApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["AdminApiKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresAdminKey(context.Request))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { message = "Admin API is not configured." });
            return;
        }

        var suppliedKey = context.Request.Headers[HeaderName].ToString();
        if (!KeysMatch(_apiKey, suppliedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "A valid admin API key is required." });
            return;
        }

        await _next(context);
    }

    private static bool RequiresAdminKey(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/api/ai") ||
            request.Path.StartsWithSegments("/api/crawler") ||
            request.Path.Equals("/api/news/unprocessed"))
        {
            return true;
        }

        return request.Method != HttpMethods.Get &&
               request.Path.StartsWithSegments("/api/news");
    }

    private static bool KeysMatch(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
