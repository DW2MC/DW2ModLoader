using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using DistantWorlds.Types;
using DistantWorlds.UI;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Engine;
using Medallion.Collections;
using Microsoft.Extensions.DependencyInjection;
using Xenko.Core.IO;
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
        if (Instance is not null)
            throw new NotSupportedException("Only one instance of ModManager is supported at this time.");

        Instance = this;

        ConsoleHelper.CreateConsole();

        //ConsoleHelper.TryEnableVirtualTerminalProcessing();

        Console.WriteLine($"Started {DateTime.UtcNow}");

        var debug = Environment.GetEnvironmentVariable("DW2MC_DEBUG");

        if (debug is not null && debug is not "")
        {
            var fs = new FileStream("debug.log", FileMode.Append, FileAccess.Write, FileShare.Read);
            var stdOut = Console.OpenStandardOutput();
            var stdErr = Console.OpenStandardError();
            var logger = new StreamWriter(fs, Encoding.UTF8, 4096, false) { AutoFlush = true };
            var conOut = new StreamWriter(stdOut, Encoding.UTF8, 4096, false) { AutoFlush = true };
            var conErr = new StreamWriter(stdErr, Encoding.UTF8, 4096, false) { AutoFlush = true };
            Console.SetOut(new TeeTextWriter(conOut, logger));
            Console.SetError(new TeeTextWriter(conErr, logger));
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) => {
            var ex = (Exception)args.ExceptionObject;
            Console.Error.WriteLine("=== === === === === === === === === ===");
            Console.Error.WriteLine("===  AppDomain Unhandled Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === ===");
            if (ex is InvalidProgramException ipe)
            {
                var st = new EnhancedStackTrace(ipe);
                var frame = st.GetFrame(0);
                Console.Error.WriteLine($"@ {frame.GetMethod().FullDescription()} + IL_{frame.GetILOffset():X4}");
            }
            Console.Error.WriteLine(ex.ToStringDemystified());
            Console.Error.WriteLine("=== === === === === === === === === === ===");
            Console.Error.WriteLine("===  End AppDomain Unhandled Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === === ===");
        };
        TaskScheduler.UnobservedTaskException += (_, args) => {
            var ex = (Exception)args.Exception;
            Console.Error.WriteLine("=== === === === === === === === ===");
            Console.Error.WriteLine("===  Unobserved Task Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === ===");
            Console.Error.WriteLine(ex.ToStringDemystified());
            Console.Error.WriteLine("=== === === === === === === === === ===");
            Console.Error.WriteLine("===  End Unobserved Task Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === ===");
        };

        try
        {
            Console.Title = $"Distant Worlds 2 Mod Loader {Version}";
        }
        catch
        {
            // ok
        }

        Console.WriteLine($"DW2 Mod Loader v{Version}");
        Console.WriteLine($"{RuntimeInformation.FrameworkDescription} on {RuntimeInformation.OSDescription}");

        UpdateCheck = new GitHubUpdateCheck("https://github.com/DW2MC/DW2ModLoader", Version);

        UpdateCheck.Start();

        UnblockUtil.UnblockFile(new Uri(typeof(ModManager).Assembly.CodeBase).LocalPath);

        new Harmony("DistantWorlds2ModLoader").PatchAll();

        UnhandledException += edi => {

            Console.Error.WriteLine("=== === === === === === === === === === === === === ===");
            Console.Error.WriteLine("===   DistantWorlds2.ModLoader Unhandled Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === === === === === ===");
            WriteStackTrace(edi);
            Console.Error.WriteLine("=== === === === === === === === === === === === === === ===");
            Console.Error.WriteLine("===   End DistantWorlds2.ModLoader Unhandled Exception  ===");
            Console.Error.WriteLine("=== === === === === === === === === === === === === === ===");
        };

        _serviceCollection = new();

        _serviceCollection.AddSingleton<IServiceProvider>(this);

        AddSingleton(typeof(IServiceProvider), this);
        AddSingleton(typeof(ModManager), this);

        Game.GameStarted += OnGameStarted;
    }

    public IUpdateCheck UpdateCheck { get; private set; }

    private void OnGameStarted(object sender, EventArgs _)
    {
        var game = (Game)sender;
        AddSingleton(typeof(IGame), game);
        AddSingleton(typeof(GameBase), game);
        AddSingleton(typeof(Game), game);
        AddSingleton(typeof(DWGame), game);
        game.GameSystems.Add(this);
        Game.GameStarted -= OnGameStarted;
    }

    private static void WriteStackTrace(ExceptionDispatchInfo edi)
    {
        var ex = edi.SourceException;

        // unwrap and discard outer TIEs
        while (ex is TargetInvocationException && ex.InnerException is not null)
            ex = ex.InnerException;

        Console.Error.WriteLine(ex.ToStringDemystified());
    }

    private static string Version => InfoVerAttrib!.InformationalVersion;

    private void LoadMods()
    {
        _gameDir = Path.GetDirectoryName(new Uri(typeof(Game).Assembly.CodeBase).LocalPath) ?? Environment.CurrentDirectory;
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

        foreach (var mod in Mods.Values)
            try
            {
                mod.ResolveDependencies(this);
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
            // TODO: assembly references for mod recursively until all loaded
            Interlocked.Exchange(ref _loadContextMod, null);
        }

        foreach (var overrideAssetsPath in OverrideAssetsQueue)
        {
            try
            {
                VirtualFileSystem.MountFileSystem(overrideAssetsPath, overrideAssetsPath);
            }
            catch (Exception ex)
            {
                OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            }
        }

        foreach (var dataPath in PatchedDataQueue)
        {
            try
            {
                GameDataDefinitionPatching.ApplyStaticDataPatches(this, dataPath);
            }
            catch (Exception ex)
            {
                OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            }
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
                return LoadAssembly(Path.Combine(ctx.Dir, ctx.MainModule!));

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

        if (fc < 60) return;

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
                (UpdateCheck.IsNewVersionAvailable ? $"New version available! ({UpdateCheck.NewVersion})" : "You have the latest version.") +
                $"{RuntimeInformation.FrameworkDescription} on {RuntimeInformation.OSDescription}\n" +
                $"GC: {(GCSettings.IsServerGC ? "Server" : "Standard")} {GCSettings.LatencyMode}\n" +
                $"Loaded: {loadedModsStrings.Length}\n" +
                $"{loadedModsMsg}\n\n" +
                $"Failed: {failedModsStrings.Length}\n" +
                $"{failedModsMsg}",
                false, new(string.Empty, "OK", HideMessageDialog, null),
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
    }
    private static void HideMessageDialog(object o, DWEventArgs dwEventArgs)
        => UserInterfaceController.HideMessageDialog();

    public bool Enabled { get; } = true;

    public int UpdateOrder => 0;

    public Queue<string> OverrideAssetsQueue { get; } = new();
    public Queue<string> PatchedDataQueue { get; } = new();

    public static ModManager Instance { get; private set; } = null!;

    public ConcurrentDictionary<string, object> SharedVariables { get; } = new();

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

    public T GetService<T>() where T : class => (T)GetService(typeof(T));
}
