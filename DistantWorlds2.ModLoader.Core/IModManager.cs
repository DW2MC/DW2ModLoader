using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Xenko.Engine;
using Xenko.Games;

namespace DistantWorlds2.ModLoader;

public interface IModManager
    : IServiceProvider, IGameSystemBase, IUpdateable, IContentable
{
    IUpdateCheck UpdateCheck { get; }

    ConcurrentDictionary<string, IModInfo> Mods { get; }

    Game Game { get; internal set; }

    Stack<string> OverrideAssetsStack { get; }

    Stack<string> PatchedDataStack { get; }

    ConcurrentDictionary<string, object> SharedVariables { get; }

    void AddScoped(Type type, Func<IServiceProvider, object> factory);

    void AddSingleton(Type type, Func<IServiceProvider, object> factory);

    void AddSingleton(Type type, object singleton);

    void AddTransient(Type type, Func<IServiceProvider, object> factory);

    T? GetService<T>() where T : class;

    void OnUnhandledException(ExceptionDispatchInfo edi);
}
