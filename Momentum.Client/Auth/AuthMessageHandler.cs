using System.Net;
using System.Net.Http.Headers;

namespace Momentum.Client.Auth;

public class AuthMessageHandler(
    TokenProvider tokenProvider,
    JwtAuthStateProvider authStateProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tokenProvider.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.Token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !string.IsNullOrWhiteSpace(tokenProvider.Token))
        {
            await authStateProvider.MarkUserAsLoggedOut();
        }

        return response;
    }
}
