using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;
using Octokit;
using Octokit.Internal;

namespace DistantWorlds2.ModLoader;

/// <summary>
/// Generic Http client. Useful for those who want to swap out System.Net.HttpClient with something else.
/// </summary>
/// <remarks>
/// Most folks won't ever need to swap this out. But if you're trying to run this on Windows Phone, you might.
/// </remarks>
public class FancyHttpClientAdapter : IHttpClient
{
    readonly HttpClient _http;

    public const string RedirectCountKey = "RedirectCount";

    [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
    public FancyHttpClientAdapter(Func<HttpMessageHandler> getHandler)
    {
        if (getHandler is null)
            throw new ArgumentNullException(nameof(getHandler));
        _http = ModLoader.HttpClientFactory.Create(new RedirectHandler { InnerHandler = getHandler() });
    }

    private class RedirectHandler : DelegatingHandler { }

    /// <summary>
    /// Sends the specified request and returns a response.
    /// </summary>
    /// <param name="request">A <see cref="IRequest"/> that represents the HTTP request</param>
    /// <param name="cancellationToken">Used to cancel the request</param>
    /// <returns>A <see cref="Task" /> of <see cref="IResponse"/></returns>
    public async Task<IResponse> Send(IRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var cancellationTokenForRequest = GetCancellationTokenForRequest(request, cancellationToken);

        using var requestMessage = BuildRequestMessage(request);

        var responseMessage = await SendAsync(requestMessage, cancellationTokenForRequest).ConfigureAwait(false);

        return await BuildResponse(responseMessage).ConfigureAwait(false);
    }

    [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
    static CancellationToken GetCancellationTokenForRequest(IRequest request, CancellationToken cancellationToken)
    {
        var cancellationTokenForRequest = cancellationToken;

        if (request.Timeout == TimeSpan.Zero)
            return cancellationTokenForRequest;

        var timeoutCancellation = new CancellationTokenSource(request.Timeout);
        var unifiedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);

        cancellationTokenForRequest = unifiedCancellationToken.Token;
        return cancellationTokenForRequest;
    }

    protected virtual async Task<IResponse> BuildResponse(HttpResponseMessage responseMessage)
    {
        if (responseMessage is null)
            throw new ArgumentNullException(nameof(responseMessage));

        object? responseBody = null;
        string? contentType = null;

        // We added support for downloading images,zip-files and application/octet-stream.
        // Let's constrain this appropriately.
        var binaryContentTypes = new[]
        {
            AcceptHeaders.RawContentMediaType,
            "application/zip",
            "application/x-gzip",
            "application/octet-stream"
        };

        using (var content = responseMessage.Content)
        {
            if (content is not null)
            {
                contentType = GetContentMediaType(responseMessage.Content);

                if (contentType is not null && (contentType.StartsWith("image/") || binaryContentTypes
                        .Any(item => item.Equals(contentType, StringComparison.OrdinalIgnoreCase))))
                    responseBody = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                else
                    responseBody = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        var responseHeaders = responseMessage.Headers.ToDictionary(h => h.Key, h => h.Value.First());

        // Add Client response received time as a synthetic header
        const string receivedTimeHeaderName = "X-Octokit-ReceivedDate";
        if (responseMessage.RequestMessage?.Properties is { } reqProperties
            && reqProperties.TryGetValue(receivedTimeHeaderName, out var receivedTimeObj)
            && receivedTimeObj is string receivedTimeString
            && !responseHeaders.ContainsKey(receivedTimeHeaderName))
            responseHeaders[receivedTimeHeaderName] = receivedTimeString;

        return new FancyResponse(
            responseMessage.StatusCode,
            responseBody,
            responseHeaders,
            contentType);
    }

    protected virtual HttpRequestMessage BuildRequestMessage(IRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        var fullUri = new Uri(request.BaseAddress, request.Endpoint);
        HttpRequestMessage requestMessage = new(request.Method, fullUri);

        foreach (var header in request.Headers)
            requestMessage.Headers.Add(header.Key, header.Value);

        switch (request.Body)
        {
            case HttpContent httpContent:
                requestMessage.Content = httpContent;
                break;

            case string body:
                requestMessage.Content = new StringContent(body, Encoding.UTF8, request.ContentType);
                break;

            case Stream bodyStream:
                requestMessage.Content = new StreamContent(bodyStream);
                requestMessage.Content.Headers.ContentType = new(request.ContentType);
                break;
        }

        return requestMessage;
    }

    static string? GetContentMediaType(HttpContent httpContent)
    {
        if (httpContent.Headers is not null && httpContent.Headers.ContentType is not null)
            return httpContent.Headers.ContentType.MediaType;
        return null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _http.Dispose();
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Clone the request/content in case we get a redirect
        var clonedRequest = await CloneHttpRequestMessageAsync(request);

        // Send initial response
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        // Need to determine time on client computer as soon as possible.
        var receivedTime = DateTimeOffset.Now;

        // Since Properties are stored as objects, serialize to HTTP round-tripping string (Format: r)
        // Resolution is limited to one-second, matching the resolution of the HTTP Date header
        request.Properties["X-Octokit-ReceivedDate"] = receivedTime.ToString("r", CultureInfo.InvariantCulture);

        // Can't redirect without somewhere to redirect to.
        if (response.Headers.Location == null)
            return response;

        // Don't redirect if we exceed max number of redirects
        var redirectCount = 0;
        if (request.Properties.Keys.Contains(RedirectCountKey))
            redirectCount = (int)request.Properties[RedirectCountKey];
        if (redirectCount > 3)
            throw new InvalidOperationException("The redirect count for this request has been exceeded. Aborting.");

        var code = response.StatusCode;

        if (code is not (HttpStatusCode.MovedPermanently
                or HttpStatusCode.Redirect
                or HttpStatusCode.Found
                or HttpStatusCode.SeeOther
                or HttpStatusCode.TemporaryRedirect) && (int)code != 308)
            return response;

        if (code == HttpStatusCode.SeeOther)
        {
            clonedRequest.Content = null;
            clonedRequest.Method = HttpMethod.Get;
        }

        // Set the new Uri based on location header
        clonedRequest.RequestUri = response.Headers.Location;

        // Increment the redirect count
        clonedRequest.Properties[RedirectCountKey] = ++redirectCount;

        // Clear authentication if redirected to a different host
        if (string.Compare(clonedRequest.RequestUri.Host, request.RequestUri.Host, StringComparison.OrdinalIgnoreCase) != 0)
            clonedRequest.Headers.Authorization = null;

        // Send redirected request
        response = await SendAsync(clonedRequest, cancellationToken).ConfigureAwait(false);

        return response;
    }

    public static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage oldRequest)
    {
        // TODO: get from factory
        var newRequest = new HttpRequestMessage(oldRequest.Method, oldRequest.RequestUri);

        // Copy the request's content (via a MemoryStream) into the cloned object
        var ms = new MemoryStream();
        if (oldRequest.Content is not null)
        {
            await oldRequest.Content.CopyToAsync(ms);
            ms.Position = 0;
            newRequest.Content = new StreamContent(ms);

            // Copy the content headers
            if (oldRequest.Content.Headers is not null)
                foreach (var h in oldRequest.Content.Headers)
                    newRequest.Content.Headers.Add(h.Key, h.Value);
        }

        newRequest.Version = oldRequest.Version;

        foreach (var header in oldRequest.Headers)
            newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var property in oldRequest.Properties)
            newRequest.Properties.Add(property);

        return newRequest;
    }

    /// <summary>
    /// Set the GitHub Api request timeout.
    /// </summary>
    /// <param name="timeout">The Timeout value</param>
    public void SetRequestTimeout(TimeSpan timeout)
        => _http.Timeout = timeout;
}
