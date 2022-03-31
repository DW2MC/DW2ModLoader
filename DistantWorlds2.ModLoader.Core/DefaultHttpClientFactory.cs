using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class DefaultHttpClientFactory : IHttpClientFactory
{
    public HttpClient Create()
        => new();

    public HttpClient Create(HttpMessageHandler handler)
        => new(handler);

    public HttpClient Create(HttpMessageHandler handler, bool disposeHandler)
        => new(handler, disposeHandler);
}
