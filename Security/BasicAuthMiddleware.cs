using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace FeuerwerkLager.Security;

public class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BasicAuthMiddleware> _logger;
    private readonly BasicAuthOptions _options;

    public BasicAuthMiddleware(RequestDelegate next, ILogger<BasicAuthMiddleware> logger, IOptions<BasicAuthOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogWarning("BasicAuth ist deaktiviert, weil keine Zugangsdaten konfiguriert wurden.");
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out var headerValues) ||
            headerValues.Count == 0 ||
            !headerValues[0].StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            await ChallengeAsync(context);
            return;
        }

        var header = headerValues[0];
        var encoded = header.Substring("Basic ".Length).Trim();
        string decoded;

        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException)
        {
            await ChallengeAsync(context);
            return;
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex <= 0)
        {
            await ChallengeAsync(context);
            return;
        }

        var username = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];

        if (!IsMatch(username, _options.Username) || !IsMatch(password, _options.Password))
        {
            await ChallengeAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool IsMatch(string input, string expected)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        if (inputBytes.Length != expectedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(inputBytes, expectedBytes);
    }

    private Task ChallengeAsync(HttpContext context)
    {
        context.Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{_options.Realm}\"";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsync("Authentication required.");
    }
}
