using System.Net;
using System.Net.Http.Headers;
using Momentum.Client.Services;

namespace Momentum.Client.Auth;

/// <summary>
/// Delegating handler that:
///   1. Attaches the Bearer token to every outgoing request.
///   2. Retries up to 3 times (2 s / 4 s / 6 s) when a network-level failure
///      occurs, broadcasting progress via <see cref="ColdStartService"/> so the
///      UI can show a "waking up the server" message.
///   3. Signs the user out when the API returns 401.
/// </summary>
public class AuthMessageHandler(
    TokenProvider tokenProvider,
    JwtAuthStateProvider authStateProvider,
    ColdStartService coldStartService) : DelegatingHandler
{
    // Delay before each successive retry attempt (ms).
    private static readonly int[] RetryDelaysMs = [2000, 4000, 6000];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Snapshot the token once — it won't change mid-flight.
        var token = tokenProvider.Token;

        // Buffer request content up-front so every retry attempt can rebuild
        // a fresh HttpRequestMessage (an HttpRequestMessage can only be sent once).
        byte[]? bodyBytes = null;
        IReadOnlyList<KeyValuePair<string, IEnumerable<string>>>? contentHeaders = null;

        if (request.Content is not null)
        {
            bodyBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            contentHeaders = request.Content.Headers.ToList();
        }

        int maxAttempts = 1 + RetryDelaysMs.Length; // 1 initial + 3 retries = 4
        Exception? lastException = null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Wait before each retry (not before the very first attempt).
            if (attempt > 0)
            {
                coldStartService.SetRetrying("Waking up the server, please wait...");
                await Task.Delay(RetryDelaysMs[attempt - 1], cancellationToken);
            }

            // Build a fresh request message for this attempt.
            var req = BuildRequest(request.Method, request.RequestUri!, token,
                                   request.Headers, bodyBytes, contentHeaders);
            try
            {
                var response = await base.SendAsync(req, cancellationToken);

                // Success — clear any retry banner and handle 401.
                if (attempt > 0)
                    coldStartService.SetIdle();

                if (response.StatusCode == HttpStatusCode.Unauthorized
                    && !string.IsNullOrWhiteSpace(token))
                {
                    await authStateProvider.MarkUserAsLoggedOut();
                }

                return response;
            }
            catch (Exception ex) when (
                ex is HttpRequestException or TaskCanceledException
                && !cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                // Loop continues to next attempt (if any remain).
            }
        }

        // All attempts exhausted.
        coldStartService.SetIdle();
        throw new HttpRequestException(
            "The server is taking longer than expected to start. Please try again in a moment.",
            lastException);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static HttpRequestMessage BuildRequest(
        HttpMethod method,
        Uri uri,
        string? token,
        System.Net.Http.Headers.HttpRequestHeaders originalHeaders,
        byte[]? bodyBytes,
        IReadOnlyList<KeyValuePair<string, IEnumerable<string>>>? contentHeaders)
    {
        var req = new HttpRequestMessage(method, uri);

        // Copy non-Authorization headers from the original request.
        foreach (var header in originalHeaders)
        {
            if (!header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                req.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Attach the auth token.
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Reconstruct the body from the buffered bytes.
        if (bodyBytes is not null && contentHeaders is not null)
        {
            req.Content = new ByteArrayContent(bodyBytes);
            foreach (var header in contentHeaders)
                req.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return req;
    }
}
