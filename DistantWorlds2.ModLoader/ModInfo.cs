using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using SharpJson;
using Xenko.Core.IO;
using Xenko.Engine;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class ModInfo
{
    public readonly string Name;
    public readonly string Dir;

    public readonly string[] Dependencies;

    public readonly string? MainModule;
    public readonly string? MainClass;

    public readonly string? DisplayName;

    private readonly ConcurrentDictionary<string, ModInfo> _resolvedDependencies = new();

    private object? _loadedMod;

    public ModInfo(string dir)
    {
        Dir = dir ?? throw new ArgumentNullException(nameof(dir));
        ResolvedDependencies = new ReadOnlyDictionary<string, ModInfo>(_resolvedDependencies);

        var jsonPath = Path.Combine(dir, "mod.json");
        if (File.Exists(jsonPath))
        {
            var depList = new List<string>();
            var jsonTxt = File.ReadAllText(jsonPath);
            var jsonObj = JsonDecoder.DecodeText(jsonTxt);
            if (jsonObj is not IDictionary<string, object> modInfo)
                Valid = false;
            else
            {
                Valid = true;
                Descriptor = new ReadOnlyDictionary<string, object>(modInfo);

                if (modInfo.TryGetValue("name", out var name))
                    if (name is string nameStr)
                        Name = nameStr;

                if (modInfo.TryGetValue("version", out var version))
                    if (version is string versionStr)
                        Version = versionStr;

                if (modInfo.TryGetValue("mainModule", out var mainModule))
                    if (mainModule is string mainModuleStr)
                    {
                        MainModule = mainModuleStr;
                        MainModuleName = MainModule.EndsWith(".dll") ? MainModule.Substring(0, MainModule.Length - 4) : MainModule;
                    }

                if (modInfo.TryGetValue("mainClass", out var mainClass))
                    if (mainClass is string mainClassStr)
                        MainClass = mainClassStr;

                if (modInfo.TryGetValue("displayName", out var displayName))
                    if (displayName is string displayNameStr)
                        DisplayName = displayNameStr;

                // TODO: min mod manager version

                // TODO: min and max game versions

                // TODO: module startup mechanism (call static load and instance class, which class, etc.)

                // TODO: alternate start module name 

                if (modInfo.TryGetValue("dependencies", out var deps))
                    if (deps is IList<object> depsArray)
                        foreach (var dep in depsArray)
                        {
                            if (dep is string depStr)
                                depList.Add(depStr);
                            // TODO: support { name: "mod name", version: "semver dependency expression" }
                        }

                if (modInfo.TryGetValue("overrideAssets", out var overrideAssets))
                    if (overrideAssets is string overrideAssetsStr)
                        OverrideAssets = overrideAssetsStr;

                Dependencies = depList.ToArray();
            }

            Console.WriteLine($"Parsed {this} from {dir}\\mod.json");
        }
        else
            Valid = false;
    }

    public string? OverrideAssets { get; }

    public string? Version { get; }

    public string? MainModuleName { get; }

    public bool Valid { get; private set; }

    public IReadOnlyDictionary<string, object> Descriptor { get; }
    public IReadOnlyDictionary<string, ModInfo> ResolvedDependencies { get; }

    public void ResolveDependencies(ModManager manager)
    {
        foreach (var dep in Dependencies)
            if (manager.Mods.TryGetValue(dep, out var info))
                _resolvedDependencies[dep] = info;
    }

    public static IEnumerable<ModInfo> GetResolvedDependencies(ModInfo mod) => mod.ResolvedDependencies.Values;

    public void Load(IServiceProvider sp)
    {
        if (OverrideAssets is not null)
        {
            var dirName = Path.GetFileName(Dir);
            var overrideAssetsPath = Path.Combine("mods", dirName, OverrideAssets)
                .Replace('\\', '/');
            sp.GetService<ModManager>()!
                .OverrideAssetQueue
                .Enqueue(overrideAssetsPath);
        }

        if (MainModule == null) return;
        var path = Path.Combine(Dir, MainModule);
        //UnblockUtil.UnblockDirectory(Dir);
        Console.WriteLine($"Loading {this} from {path}");
        var asm = ModManager.LoadAssembly(path);
        LoadedMainModule = asm;

        if (MainClass == null) return;
        var modType = asm.GetType(MainClass, false);
        if (modType == null)
        {
            Valid = false;
            return;
        }

        if (modType.IsAbstract)
        {
            try
            {
                RuntimeHelpers.RunClassConstructor(modType.TypeHandle);
            }
            catch (Exception ex)
            {
                var edi = ExceptionDispatchInfo.Capture(ex);
                ModManager.OnUnhandledException(edi);
                Valid = false;
            }
            _loadedMod = modType;
        }
        else
            try
            {
                ActivatorUtilities.CreateInstance(sp, modType);
            }
            catch (Exception ex)
            {
                var edi = ExceptionDispatchInfo.Capture(ex);
                ModManager.OnUnhandledException(edi);
                Valid = false;
            }
    }

    public Assembly? LoadedMainModule { get; private set; }

    public override string ToString()
    {
        var name = DisplayName ?? Name;
        return Version is not null
            ? $"{name} v{Version}"
            : name;
    }
}
