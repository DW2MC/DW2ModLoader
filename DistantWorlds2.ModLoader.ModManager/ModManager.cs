using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityPatch;
using Cysharp.Text;
using DistantWorlds.Types;
using DistantWorlds.UI;
using JetBrains.Annotations;
using Xenko.Engine;
using Medallion.Collections;
using Microsoft.Extensions.DependencyInjection;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Games;
using System.IO.Hashing;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class ModManager : IModManager
{
    private int _refCount;
    private string? _gameDir;
    private string? _modsDir;
    private IEnumerable<IModInfo>? _loadOrder;
    private IModInfo? _loadContextMod;

    private readonly TaskCompletionSource<object> _tcsGame = new();

    public ModManager()
    {
        var hasher = new XxHash64();
        DataUtils.ComputeFileHash(hasher, "./DistantWorlds2.ModLoader.dll");
        Console.WriteLine(Encoding.UTF8.GetString(hasher.GetCurrentHash()));
        Console.WriteLine($"Mod Manager started {DateTime.UtcNow}");

        ModLoader.Patches.Run();

        AppDomain.CurrentDomain.UnhandledException += (_, args) => {
            if (CallStackHelpers.GetCallStackDepth() > 32) return;
            var sb = ZString.CreateStringBuilder();
            try
            {
                var ex = (Exception)args.ExceptionObject;
                sb.AppendLine("=== === === === === === === === === ===");
                sb.AppendLine("===  AppDomain Unhandled Exception  ===");
                sb.AppendLine("=== === === === === === === === === ===");
                try
                {
                    if (ex is InvalidProgramException ipe)
                    {
                        var st = new EnhancedStackTrace(ipe);
                        var frame = st.GetFrame(0);

                        var methodBase = frame.GetMethod();
                        var methodName = methodBase.Name;
                        if (methodBase is MethodInfo mi) methodName = $"{mi.ReflectedType?.FullName ?? "???"}.{methodName}";
                        sb.AppendLine($"@ {methodName} + IL_{frame.GetILOffset():X4}");
                    }
                }
                catch
                {
                    sb.AppendLine("Can't diagnose proximity of offending IL offset");
                }
                ExplainException(ex, ref sb);
                sb.AppendLine("=== === === === === === === === === === ===");
                sb.AppendLine("===  End AppDomain Unhandled Exception  ===");
                sb.AppendLine("=== === === === === === === === === === ===");
                var seg = sb.AsArraySegment();
                Console.Error.WriteLine(seg.Array!, seg.Offset, seg.Count);
            }
            catch
            {
                var seg = sb.AsArraySegment();
                Console.Error.WriteLine(seg.Array!, seg.Offset, seg.Count);
                Console.Error.WriteLine(
                    "=== === === === === === === === === === === === === ===\n" +
                    "=== AppDomain Unhandled Exception Failed To Report  ===\n" +
                    "=== === === === === === === === === === === === === ===");
            }
        };

        if (ModLoader.DebugMode)
            AppDomain.CurrentDomain.FirstChanceException += (_, args) => {
                if (CallStackHelpers.GetCallStackDepth() > 32) return;

                var sb = ZString.CreateStringBuilder();
                try
                {
                    var ex = args.Exception;
                    sb.AppendLine("=== === === === === === === === === === ===");
                    sb.AppendLine("===   AppDomain First Chance Exception  ===");
                    sb.AppendLine("=== === === === === === === === === === ===");
                    try
                    {
                        if (ex is InvalidProgramException ipe)
                        {
                            var st = new EnhancedStackTrace(ipe);
                            var frame = st.GetFrame(0);

                            var methodBase = frame.GetMethod();
                            var methodName = methodBase.Name;
                            if (methodBase is MethodInfo mi) methodName = $"{mi.ReflectedType?.FullName ?? "???"}.{methodName}";
                            sb.AppendLine($"@ {methodName} + IL_{frame.GetILOffset():X4}");
                        }
                    }
                    catch
                    {
                        sb.AppendLine("Can't diagnose proximity of offending IL offset");
                    }
                    ExplainException(ex, ref sb);
                    sb.AppendLine("=== === === === === === === === === === === ===");
                    sb.AppendLine("===  End AppDomain First Chance Exception   ===");
                    sb.AppendLine("=== === === === === === === === === === === ===");
                    var seg = sb.AsArraySegment();
                    Console.Error.WriteLine(seg.Array!, seg.Offset, seg.Count);
                }
                catch
                {
                    var seg = sb.AsArraySegment();
                    Console.Error.WriteLine(seg.Array!, seg.Offset, seg.Count);
                    Console.Error.WriteLine(
                        "=== === === === === === === === === === === === === === ===\n" +
                        "===  AppDomain First Chance Exception Failed To Report  ===\n" +
                        "=== === === === === === === === === === === === === === ===");
                }
            };

        TaskScheduler.UnobservedTaskException += (_, args) => {
            // oof
            args.SetObserved();
            if (CallStackHelpers.GetCallStackDepth() > 32) return;
            var sb = ZString.CreateStringBuilder();
            try
            {

                var ex = (Exception)args.Exception;
                sb.AppendLine("=== === === === === === === === ===");
                sb.AppendLine("===  Unobserved Task Exception  ===");
                sb.AppendLine("=== === === === === === === === ===");
                ExplainException(ex, ref sb);
                sb.AppendLine("=== === === === === === === === === ===");
                sb.AppendLine("===  End Unobserved Task Exception  ===");
                sb.AppendLine("=== === === === === === === === === ===");
                var seg = sb.AsArraySegment();
                Console.Error.WriteLine(seg.Array!, seg.Offset, seg.Count);
            }
            catch
            {
                var seg = sb.AsArraySegment();
                Console.Error.WriteLine(seg.Array!, seg.Offset, seg.Count);
                Console.Error.WriteLine(
                    "=== === === === === === === === === === === === ===\n" +
                    "=== Unobserved Task Exception Failed To Report  ===\n" +
                    "=== === === === === === === === === === === === ===");
            }
        };

        Console.WriteLine("Registering for GameStarted event...");
        Game.GameStarted += OnGameStarted;

        try
        {
            if (ModLoader.DebugMode)
                Console.Title = $"Distant Worlds 2 Mod Loader {Version}";
        }
        catch
        {
            // ok
        }

        Console.WriteLine($"DW2 Mod Loader v{Version}");
        Console.WriteLine($"{RuntimeInformation.FrameworkDescription} on {RuntimeInformation.OSDescription}");

        _serviceCollection = new();

        _serviceCollection.AddSingleton<IServiceProvider>(this);

        AddSingleton(typeof(IServiceProvider), this);
        AddSingleton(typeof(IModManager), this);
        AddSingleton(typeof(ModManager), this);
        AddSingleton(typeof(IHttpClientFactory), ModLoader.HttpClientFactory);
        AddTransient(typeof(HttpClient), p => p.GetService<IHttpClientFactory>()?.Create()!);
        AddTransient(typeof(HttpMessageInvoker), p => p.GetService<HttpClient>()!);
        AddTransient(typeof(Task<IGame>), _ => _tcsGame.Task.ContinueWith(t => (IGame)t.Result));
        AddTransient(typeof(Task<GameBase>), _ => _tcsGame.Task.ContinueWith(t => (GameBase)t.Result));
        AddTransient(typeof(Task<Game>), _ => _tcsGame.Task.ContinueWith(t => (Game)t.Result));
        AddTransient(typeof(Task<DWGame>), _ => _tcsGame.Task.ContinueWith(t => (DWGame)t.Result));

        UpdateCheck = new GitHubUpdateCheck("https://github.com/DW2MC/DW2ModLoader", Version);

        ModLoader.Ready.Set();

        ThreadPool.UnsafeQueueUserWorkItem(LoadModsWorker, null);

        UpdateCheck.Start();
    }
    private void LoadModsWorker(object _)
    {
        try
        {
            LoadMods();

            LoadModModules();

            ModLoader.ModModulesLoaded.Set();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to load mods!");
            ModLoader.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
        }
    }

    private static void ExplainException(Exception ex, ref Utf16ValueStringBuilder sb)
    {
        try
        {
            sb.AppendLine(ex.ToStringDemystified());
        }
        catch
        {
            try
            {
                sb.AppendLine(ex.ToString());
            }
            catch
            {
                sb.AppendLine(ex.GetType().AssemblyQualifiedName);
                sb.AppendLine("Failed to describe exception.");
            }
        }
    }

    public IUpdateCheck UpdateCheck { get; private set; }

    private void OnGameStarted(object sender, EventArgs _)
    {
        if (ModLoader.IntentionallyFail)
        {
            Console.WriteLine("Unregistering for GameStarted event...");
            Game.GameStarted -= OnGameStarted;
            ModLoader.IntentionallyFail = false;
            throw new InvalidOperationException("Intentional failure.");
        }

        var game = Game = (Game)sender;
        game.GameSystems.Add(this);
        Console.WriteLine("Unregistering for GameStarted event...");
        Game.GameStarted -= OnGameStarted;

        if (ModLoader.DebugMode)
        {
            game.ConsoleLogLevel = LogMessageType.Debug;
            game.ConsoleLogMode = ConsoleLogMode.Always;
        }
        ModLoader.GameStarted.Set();

        ThreadPool.UnsafeQueueUserWorkItem(_ => {
            LoadModClasses();
            ModLoader.ModClassesLoaded.Set();
        }, null);
    }
    private void LoadModModules()
    {
        Console.WriteLine($"Loading modification modules...");
        if (_loadOrder is null) return;
        foreach (var mod in _loadOrder)
        {
            Interlocked.Exchange(ref _loadContextMod, mod);
            try
            {
                mod.InitializeModClass(this);
            }
            catch (Exception ex)
            {
                var edi = ExceptionDispatchInfo.Capture(ex);
                OnUnhandledException(edi);
            }
            // TODO: assembly references for mod recursively until all loaded
            Interlocked.Exchange(ref _loadContextMod, null);
        }
    }
    private void LoadModClasses()
    {
        Console.WriteLine($"Loading modification modules...");
        if (_loadOrder is null) return;
        foreach (var mod in _loadOrder)
        {
            Interlocked.Exchange(ref _loadContextMod, mod);
            try
            {
                mod.CreateModInstance(this);
            }
            catch (Exception ex)
            {
                var edi = ExceptionDispatchInfo.Capture(ex);
                OnUnhandledException(edi);
            }
            // TODO: assembly references for mod recursively until all loaded
            Interlocked.Exchange(ref _loadContextMod, null);
        }
    }

    public static void WriteStackTrace(ExceptionDispatchInfo edi, ref Utf16ValueStringBuilder sb)
    {
        var ex = edi.SourceException;

        // unwrap and discard outer TIEs
        while (ex is TargetInvocationException && ex.InnerException is not null)
            ex = ex.InnerException;

        ExplainException(ex, ref sb);
    }

    private static string Version => InfoVerAttrib!.InformationalVersion;

    private void LoadMods()
    {
        _gameDir = Path.GetDirectoryName(new Uri(typeof(Game).Assembly.EscapedCodeBase).LocalPath) ?? Environment.CurrentDirectory;
        _modsDir = Path.Combine(_gameDir, "Mods");

        Parallel.ForEach(Directory.GetDirectories(_modsDir), modDir => {
            try
            {
                var modInfo = LoadModInfo(modDir);
                if (modInfo == null) return;
                modInfo.UpdateHash();
                var modName = modInfo!.Name;
                Mods.TryAdd(modName, modInfo);
            }
            catch (Exception ex)
            {
                var edi = ExceptionDispatchInfo.Capture(ex);
                OnUnhandledException(edi);
            }
        });

        foreach (var mod in Mods.Values.Where(m => m.IsValid))
            try
            {
                mod.ResolveDependencies();
            }
            catch
            {
                // TODO: log
            }

        _loadOrder = Mods.Values.Where(m => m.IsValid)
            .OrderByDescending(m => m.LoadPriority)
            .StableOrderTopologicallyBy(ModInfo.GetResolvedDependencies);

        AppDomain.CurrentDomain.AssemblyResolve += ModAssemblyResolver;

        foreach (var mod in _loadOrder)
        {
            try
            {
                mod.Load(this);
            }
            catch (Exception ex)
            {
                var edi = ExceptionDispatchInfo.Capture(ex);
                OnUnhandledException(edi);
            }
        }

        foreach (var overrideAssetsPath in OverrideAssetsStack)
        {
            try
            {
                if (ModLoader.DebugMode)
                    Console.WriteLine($"Mounting {overrideAssetsPath}");
                VirtualFileSystem.MountFileSystem(overrideAssetsPath, overrideAssetsPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failure mounting {overrideAssetsPath}");
                OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            }
        }

        ModLoader.Loaded.Set();
    }

    private Assembly? ModAssemblyResolver(object sender, ResolveEventArgs args)
    {
        var ctx = Interlocked.CompareExchange(ref _loadContextMod, null, null);

        var asm = args.RequestingAssembly;

        if (asm == null)
            asm = ctx?.LoadedMainModule;

        var name = new AssemblyName(args.Name).Name;

        if (asm == null)
        {
            if (ctx is not null && name == ctx.MainModuleName)
                return LoadAssembly(Path.Combine(ctx.Dir, ctx.MainModule!));

            return null;
        }

        // check mod in context first
        if (ctx is not null)
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
        ModLoader.Unblocker.UnblockFile(path);
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

    public ConcurrentDictionary<string, IModInfo> Mods { get; } = new();

    internal Game Game
    {
        get => GetService<Game>()!;
        set {
            if (!_tcsGame.TrySetResult(value))
                throw new InvalidOperationException("Game has already been set.");
            AddSingleton(typeof(IGame), value);
            AddSingleton(typeof(GameBase), value);
            AddSingleton(typeof(Game), value);
            AddSingleton(typeof(DWGame), value);
        }
    }

    Game IModManager.Game
    {
        get => Game;
        set => Game = value;
    }

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

        if (fc <= 30) return;

        if (!ModLoader.Ready.IsSet)
        {
            if (ModLoader.DebugMode)
                Console.Error.WriteLine("Ready to show mods loaded dialog but ModLoader.Ready is not set!");
            return;
        }

        if (!ModLoader.Loaded.IsSet)
        {
            if (ModLoader.DebugMode)
                Console.Error.WriteLine("Ready to show mods loaded dialog but ModLoader.Loaded is not set!");
            return;
        }

        if (!ModLoader.ModModulesLoaded.IsSet)
        {
            if (ModLoader.DebugMode)
                Console.Error.WriteLine("Ready to show mods loaded dialog but ModLoader.ModModulesLoaded is not set!");
            return;
        }

        if (!ModLoader.ModClassesLoaded.IsSet)
        {
            if (ModLoader.DebugMode)
                Console.Error.WriteLine("Ready to show mods loaded dialog but ModLoader.ModClassesLoaded is not set!");
            return;
        }

        if (UserInterfaceController.MessageDialog == null) return;

        var renderer = Renderer;
        if (renderer is not { IsInitialized: true }) return;

        var size = UserInterfaceHelper.CalculateScaledValue(new Vector2(800f, 1000f));
        var loadedModsStrings = Mods.Values.Where(m => m.IsValid).Select(m => m.ToString(true)).ToArray();
        var loadedModsMsg = string.Join("\n", loadedModsStrings);
        var failedModsStrings = Mods.Values.Where(m => !m.IsValid).Select(m => m.ToString()).ToArray();
        var failedModsMsg = string.Join("\n", failedModsStrings);

        try
        {
            UserInterfaceController.ShowMessageDialogCentered(null, null,
                ImageFill.Zoom,
                "DW2 Mod Loader",
                $"Version v{Version}\n" +
                (UpdateCheck.IsNewVersionAvailable
                    ? $"New version available! ({UpdateCheck.NewVersion})\n"
                    : "You have the latest version.\n") +
                $"{RuntimeInformation.FrameworkDescription} on {RuntimeInformation.OSDescription}\n" +
                $"GC: {(GCSettings.IsServerGC ? "Server" : "Standard")} {GCSettings.LatencyMode}\n" +
                $"AppContainer Isolation Security: {(ModLoader.IsIsolated ? "Enabled :)" : "Disabled :\\")}\n\n" +
                $"Loaded: {loadedModsStrings.Length}\n" +
                $"{loadedModsMsg}\n\n" +
                $"Failed: {failedModsStrings.Length}\n" +
                $"{failedModsMsg}",
                true, new(string.Empty, "OK", HideMessageDialog, null),
                null,
                UserInterfaceController.ScreenWidth, UserInterfaceController.ScreenHeight, size);
        }
        catch (Exception ex)
        {
            OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            return;
        }

        Console.WriteLine($"Modifications Loaded: {loadedModsStrings.Length}");
        if (loadedModsStrings.Any())
            foreach (var loadedMod in loadedModsStrings)
                Console.WriteLine(loadedMod);
        else
            Console.WriteLine("None.");
        Console.WriteLine();
        Console.WriteLine($"Modifications Failed: {failedModsStrings.Length}");
        if (failedModsStrings.Any())
            foreach (var failedMod in failedModsStrings)
                Console.WriteLine(failedMod);
        else
            Console.WriteLine("None.");
        Console.WriteLine();

        AddSingleton(typeof(ScaledRenderer), renderer);
        _capturedRenderer = true;

        for (var i = 0; i < 3; ++i)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
    }
    private static void HideMessageDialog(object o, DWEventArgs dwEventArgs)
        => UserInterfaceController.HideMessageDialog();

    public bool Enabled { get; } = true;

    public int UpdateOrder => 0;

    public Stack<string> OverrideAssetsStack { get; } = new();
    public Stack<string> PatchedDataStack { get; } = new();

    public ConcurrentDictionary<string, object> SharedVariables { get; } = new();

    public event EventHandler<EventArgs>? EnabledChanged;

    public event EventHandler<EventArgs>? UpdateOrderChanged;

    private bool _contentLoaded;

    public void LoadContent()
    {
        if (_contentLoaded) return;
        _contentLoaded = true;

        var game = Game;

        AddSingleton(typeof(IGameSystemCollection), game.GameSystems);

        foreach (var system in game.GameSystems)
            AddSingleton(system.GetType(), system);

        foreach (var dataPath in PatchedDataStack)
        {
            try
            {
                if (ModLoader.DebugMode)
                    Console.WriteLine($"Applying content patches from {dataPath}");
                GameDataDefinitionPatching.ApplyContentPatches(dataPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failure applying content patches from {dataPath}");
                OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            }
        }
    }

    public void UnloadContent()
    {
        // TODO: trigger unload of mods?
    }

    private ServiceCollection _serviceCollection;
    private IServiceProvider? _serviceProvider;

    private static readonly AssemblyInformationalVersionAttribute? InfoVerAttrib
        = typeof(ModManager).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

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

    public T? GetService<T>() where T : class => (T)GetService(typeof(T));

    public void OnUnhandledException(ExceptionDispatchInfo edi)
        => ModLoader.OnUnhandledException(edi);

    protected virtual void OnEnabledChanged()
        => EnabledChanged?.Invoke(this, EventArgs.Empty);

    protected virtual void OnUpdateOrderChanged()
        => UpdateOrderChanged?.Invoke(this, EventArgs.Empty);
}
