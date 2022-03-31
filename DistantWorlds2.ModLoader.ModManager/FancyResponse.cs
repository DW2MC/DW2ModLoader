using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Octokit;

namespace DistantWorlds2.ModLoader;

internal class FancyResponse : IResponse
{
    private static readonly Regex RxLinkRel = new(@"rel=""(next|prev|first|last)""", RxOpts);

    private static readonly Regex RxLinkUri = new(@"<(.+)>", RxOpts);

    const RegexOptions RxOpts
        = RegexOptions.Compiled
        | RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant;

    public FancyResponse(HttpStatusCode statusCode, object? body, IDictionary<string, string> headers, string? contentType)
    {
        if (headers is null)
            throw new ArgumentNullException(nameof(headers));

        StatusCode = statusCode;
        Body = body;
        Headers = new ReadOnlyDictionary<string, string>(headers);
        ApiInfo = ParseResponseHeaders(headers);
        ContentType = contentType;
    }

    private static KeyValuePair<string, string> LookupHeader(IDictionary<string, string> headers, string key)
        => headers.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase));

    private static bool IsDefault(KeyValuePair<string, string> kvp)
        => !kvp.Equals(default(KeyValuePair<string, string>));

    private static ApiInfo ParseResponseHeaders(IDictionary<string, string> responseHeaders)
    {
        if (responseHeaders is null)
            throw new ArgumentNullException(nameof(responseHeaders));

        var httpLinks = new Dictionary<string, Uri>();
        var oauthScopes = new List<string>();
        var acceptedOauthScopes = new List<string>();
        string? etag = null;

        var acceptedOauthScopesKey = LookupHeader(responseHeaders, "X-Accepted-OAuth-Scopes");
        if (IsDefault(acceptedOauthScopesKey))
            acceptedOauthScopes.AddRange(acceptedOauthScopesKey.Value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()));

        var oauthScopesKey = LookupHeader(responseHeaders, "X-OAuth-Scopes");
        if (IsDefault(oauthScopesKey))
            oauthScopes.AddRange(oauthScopesKey.Value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()));

        var etagKey = LookupHeader(responseHeaders, "ETag");
        if (IsDefault(etagKey))
            etag = etagKey.Value;

        var linkKey = LookupHeader(responseHeaders, "Link");
        if (IsDefault(linkKey))
        {
            var links = linkKey.Value.Split(',');
            foreach (var link in links)
            {
                var relMatch = RxLinkRel.Match(link);
                if (!relMatch.Success || relMatch.Groups.Count != 2) break;

                var uriMatch = RxLinkUri.Match(link);
                if (!uriMatch.Success || uriMatch.Groups.Count != 2) break;

                httpLinks.Add(relMatch.Groups[1].Value, new(uriMatch.Groups[1].Value));
            }
        }

        var receivedTimeKey = LookupHeader(responseHeaders, "X-Octokit-ReceivedDate");
        var serverTimeKey = LookupHeader(responseHeaders, "Date");
        var serverTimeSkew = TimeSpan.Zero;
        if (DateTimeOffset.TryParse(receivedTimeKey.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var receivedTime)
            && DateTimeOffset.TryParse(serverTimeKey.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var serverTime))
            serverTimeSkew = serverTime - receivedTime;

        return new(httpLinks, oauthScopes, acceptedOauthScopes, etag, new(responseHeaders), serverTimeSkew);
    }

    /// <summary>
    /// Raw response body. Typically a string, but when requesting images, it will be a byte array.
    /// </summary>
    public object? Body { get; }

    /// <summary>
    /// Information about the API.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Information about the API response parsed from the response headers.
    /// </summary>
    public ApiInfo ApiInfo { get; }

    /// <summary>
    /// The response status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The content type of the response.
    /// </summary>
    public string? ContentType { get; }
}
