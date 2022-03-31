using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class DelegateHttpClientFactory : IHttpClientFactory
{
    private readonly Func<HttpClient> _create0;
    private readonly Func<HttpMessageHandler, HttpClient> _create1;
    private readonly Func<HttpMessageHandler, bool, HttpClient> _create2;

    public DelegateHttpClientFactory(Func<HttpClient> create0, Func<HttpMessageHandler, HttpClient> create1,
        Func<HttpMessageHandler, bool, HttpClient> create2)
    {
        _create0 = create0 ?? throw new ArgumentNullException(nameof(create0));
        _create1 = create1 ?? throw new ArgumentNullException(nameof(create1));
        _create2 = create2 ?? throw new ArgumentNullException(nameof(create2));
    }

    public HttpClient Create()
        => _create0();
    public HttpClient Create(HttpMessageHandler handler)
        => _create1(handler);
    public HttpClient Create(HttpMessageHandler handler, bool disposeHandler)
        => _create2(handler, disposeHandler);
    
    public static void Inject(Func<HttpClient> create0, Func<HttpMessageHandler, HttpClient> create1,
        Func<HttpMessageHandler, bool, HttpClient> create2)
        => ModLoader.HttpClientFactory = new DelegateHttpClientFactory(create0, create1, create2);
}
