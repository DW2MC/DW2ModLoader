namespace DistantWorlds2.ModLoader;

public interface IHttpClientFactory
{
    HttpClient Create();

    HttpClient Create(HttpMessageHandler handler);

    HttpClient Create(HttpMessageHandler handler, bool disposeHandler);
}
