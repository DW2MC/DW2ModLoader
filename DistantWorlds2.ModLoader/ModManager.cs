using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using DistantWorlds.Types;
using DistantWorlds.UI;
using JetBrains.Annotations;
using Xenko.Engine;
using Medallion.Collections;
using Microsoft.Extensions.DependencyInjection;
using Xenko.Core.Mathematics;
using Xenko.Games;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class ModManager : IServiceProvider, IGameSystemBase, IUpdateable, IContentable
{
    private int _refCount;
    private string? _gameDir;
    private string? _modsDir;
    private IEnumerable<ModInfo>? _loadOrder;
    private ModInfo? _loadContextMod;

    public ModManager()
    {
        UnblockUtil.UnblockFile(new Uri(typeof(ModManager).Assembly.CodeBase).LocalPath);

        UnhandledException += edi => {

            Console.Error.WriteLine("=== === === === === === === === === === === === === ===");
            Console.Error.WriteLine("===   DistantWorlds2.ModLoader Unhandled Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === === === === === ===");
            Console.Error.WriteLine(edi.SourceException.ToStringDemystified());
            Console.Error.WriteLine("=== === === === === === === === === === === === === === ===");
            Console.Error.WriteLine("===   End DistantWorlds2.ModLoader Unhandled Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === === === === === === ===");
        };

        _serviceCollection = new();

        _serviceCollection.AddSingleton<IServiceProvider>(this);

        Game.GameStarted += (sender, _) => {
            var game = (Game)sender;
            AddSingleton(typeof(IGame), game);
            AddSingleton(typeof(GameBase), game);
            AddSingleton(typeof(Game), game);
            AddSingleton(typeof(DWGame), game);
            game.GameSystems.Add(this);
        };
    }

    private void LoadMods()
    {
        _gameDir = Path.GetDirectoryName(new Uri(typeof(Game).Assembly.CodeBase).LocalPath) ?? Environment.CurrentDirectory;
        _modsDir = Path.Combine(_gameDir, "Mods");
        foreach (var modDir in Directory.GetDirectories(_modsDir))
        {
            try
            {
                var modInfo = LoadModInfo(modDir);
                var modName = modInfo!.Name;
                Mods.TryAdd(modName, modInfo);
            }
            catch (Exception ex)
            {
                var edi = ExceptionDispatchInfo.Capture(ex);
                OnUnhandledException(edi);
            }
        }

        foreach (var mod in Mods.Values)
            try
            {
                mod.ResolveDependencies(this);
            }
            catch
            {
                // TODO: log
            }

        _loadOrder = Mods.Values.StableOrderTopologicallyBy(ModInfo.GetResolvedDependencies);

        AppDomain.CurrentDomain.AssemblyResolve += ModAssemblyResolver;

        foreach (var mod in _loadOrder)
        {
            Interlocked.Exchange(ref _loadContextMod, mod);
            try
            {
                mod.Load(this);
            }
            catch (Exception ex)
            {
                var edi = ExceptionDispatchInfo.Capture(ex);
                OnUnhandledException(edi);
            }
            Interlocked.Exchange(ref _loadContextMod, null);
        }
    }

    public static event Action<ExceptionDispatchInfo>? UnhandledException;
    internal static void OnUnhandledException(ExceptionDispatchInfo edi)
        => UnhandledException?.Invoke(edi);

    private Assembly? ModAssemblyResolver(object sender, ResolveEventArgs args)
    {
        var ctx = Interlocked.CompareExchange(ref _loadContextMod, null, null);

        var asm = args.RequestingAssembly;

        if (asm == null)
            asm = ctx?.LoadedMainModule;

        var name = new AssemblyName(args.Name).Name;

        if (asm == null)
        {
            if (ctx != null && name == ctx.MainModuleName)
                return LoadAssembly(Path.Combine(ctx.Dir, ctx.MainModule));

            return null;
        }

        // check mod in context first
        if (ctx != null)
            if (ctx.LoadedMainModule == asm)
            {
                // load modules from mod's directory
                var path = Path.Combine(ctx.Dir, name + ".dll");

                if (File.Exists(path))
                    return LoadAssembly(path);

                // allow loading modules from mods depended upon
                foreach (var depMod in ctx.ResolvedDependencies.Values)
                {
                    path = Path.Combine(depMod.Dir, name + ".dll");

                    if (File.Exists(path))
                        return LoadAssembly(path);
                }

            }

        // check other mods
        foreach (var mod in Mods.Values)
        {
            if (mod == ctx)
                continue;

            if (mod.LoadedMainModule != asm)
                continue;

            // load modules from mod's directory
            var path = Path.Combine(mod.Dir, name + ".dll");

            if (File.Exists(path))
                return LoadAssembly(path);

            // allow loading modules from mods depended upon
            foreach (var depMod in mod.ResolvedDependencies.Values)
            {
                if (depMod == ctx)
                    continue;

                path = Path.Combine(depMod.Dir, name + ".dll");

                if (File.Exists(path))
                    return LoadAssembly(path);
            }

            break;
        }

        return null;
    }

    public static Assembly LoadAssembly(string path)
    {
        UnblockUtil.UnblockFile(path);
        return Assembly.LoadFile(path);
    }

    private ModInfo? LoadModInfo(string dir)
    {
        try
        {
            return new(dir);
        }
        catch (Exception ex)
        {
            var edi = ExceptionDispatchInfo.Capture(ex);
            OnUnhandledException(edi);
            return null;
        }
    }

    public ConcurrentDictionary<string, ModInfo> Mods { get; } = new();

    public Game Game => GetService<Game>()
        ?? throw new InvalidOperationException("Game not started yet.");

    public int AddReference() => Interlocked.Increment(ref _refCount);

    public int Release() => Interlocked.Decrement(ref _refCount);

    public int ReferenceCount => Interlocked.CompareExchange(ref _refCount, 0, 0);

    public string Name => "Mod Manager";

    public void Initialize() { }

    private ScaledRenderer? Renderer
    {
        get {
            var game = Game;
            var renderer = (ScaledRenderer)game.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .First(f => f.FieldType == typeof(ScaledRenderer))
                .GetValue(game);
            return renderer;
        }
    }


    private bool _capturedRenderer;
    public void Update(GameTime gameTime)
    {
        if (_capturedRenderer) return;

        var fc = gameTime.FrameCount;

        if (fc < 30) return;

        if (UserInterfaceController.MessageDialog == null) return;

        var renderer = Renderer;
        if (renderer is not { IsInitialized: true }) return;

        var size = UserInterfaceHelper.CalculateScaledValue(new Vector2(500f, 250f));
        var loadedMods = string.Join("\n\n", Mods.Values.Where(m => m.Valid));
        try
        {
            UserInterfaceController.ShowMessageDialogCentered(null, null,
                ImageFill.Zoom,
                "Loaded Modifications", $"\n{loadedMods}\n\n",
                true, new(string.Empty, "OK", HideMessageDialog, null),
                null,
                UserInterfaceController.ScreenWidth, UserInterfaceController.ScreenHeight, size);
        }
        catch (Exception ex)
        {
            OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            return;
        }

        AddSingleton(typeof(ScaledRenderer), renderer);
        _capturedRenderer = true;
    }
    private static void HideMessageDialog(object o, DWEventArgs dwEventArgs)
        => UserInterfaceController.HideMessageDialog();

    public bool Enabled { get; } = true;

    public int UpdateOrder => 0;

    public event EventHandler<EventArgs>? EnabledChanged;
    public event EventHandler<EventArgs>? UpdateOrderChanged;

    public void LoadContent()
    {
        var game = Game;

        AddSingleton(typeof(IGameSystemCollection), game.GameSystems);

        foreach (var system in game.GameSystems)
            AddSingleton(system.GetType(), system);

        LoadMods();
    }

    public void UnloadContent()
    {
        // TODO: trigger unload of mods?
    }

    private ServiceCollection _serviceCollection;
    private IServiceProvider? _serviceProvider;
    private bool _visible;
    private int _drawOrder;

    public void AddScoped(Type type, Func<IServiceProvider, object> factory)
    {
        _serviceCollection.AddScoped(type, factory);
        _serviceProvider = null;
    }

    public void AddSingleton(Type type, Func<IServiceProvider, object> factory)
    {
        _serviceCollection.AddSingleton(type, factory);
        _serviceProvider = null;
    }

    public void AddSingleton(Type type, object singleton)
    {
        _serviceCollection.AddSingleton(type, singleton);
        _serviceProvider = null;
    }

    public void AddTransient(Type type, Func<IServiceProvider, object> factory)
    {
        _serviceCollection.AddTransient(type, factory);
        _serviceProvider = null;
    }

    public object GetService(Type serviceType)
        => (_serviceProvider ??= _serviceCollection.BuildServiceProvider())
            .GetService(serviceType);

    public T GetService<T>() where T : class => (T)GetService(typeof(T));
}
