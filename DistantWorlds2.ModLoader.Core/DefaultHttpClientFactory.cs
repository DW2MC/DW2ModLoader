using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class DefaultHttpClientFactory : IHttpClientFactory
{
    public HttpClient Create()
        => new HttpClient();
    public HttpClient Create(HttpMessageHandler handler)
        => new HttpClient(handler);
    public HttpClient Create(HttpMessageHandler handler, bool disposeHandler)
        => new HttpClient(handler, disposeHandler);
}