using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Hashing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using SharpJson;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
public class ModInfo : IModInfo
{
    public static Type HasherType = typeof(Crc64); //TODO: Switch back to XxHash64 once https://github.com/dotnet/runtime/issues/69184 is resolved and released.

    public string Name { get; }
    public string Dir { get; }

    public string[] Dependencies { get; }

    public string? MainModule { get; }
    public string? Net4Module { get; }
    public string? Net6Module { get; }
    public string? MainClass { get; }

    public string? DisplayName { get; }

    private readonly ConcurrentDictionary<string, IModInfo> _resolvedDependencies
        = new(StringComparer.Ordinal);

    private object? _loadedMod;
    private byte[]? _hash;
    private string? _hashString;
    private readonly Dictionary<string, string> _manifest
        = new(StringComparer.OrdinalIgnoreCase);
    private Type? _modType;

    public ModInfo(string dir)
    {
        Dir = dir ?? throw new ArgumentNullException(nameof(dir));
        ResolvedDependencies = new ReadOnlyDictionary<string, IModInfo>(_resolvedDependencies);
        Manifest = new ReadOnlyDictionary<string, string>(_manifest);

        Name = Path.GetFileName(Dir)!;

        var jsonPath = Path.Combine(dir, "mod.json");
        if (File.Exists(jsonPath))
        {
            Console.WriteLine($"Parsing mod.json for {Name}");
            var depList = new List<string>();
            var jsonTxt = File.ReadAllText(jsonPath);
            var jsonObj = JsonDecoder.DecodeText(jsonTxt);
            if (jsonObj is not IDictionary<string, object> modInfo)
            {
                Console.WriteLine("Invalid mod.json");
                IsValid = false;
                Dependencies = Array.Empty<string>();
                Descriptor = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            }
            else
            {
                IsValid = true;
                Descriptor = new ReadOnlyDictionary<string, object>(modInfo);

                if (modInfo.TryGetValue("name", out var name))
                    if (name is string nameStr)
                        Name = nameStr;

                if (modInfo.TryGetValue("version", out var version))
                    if (version is string versionStr)
                        Version = versionStr;

                if (modInfo.TryGetValue("repoUri", out var repoUri))
                    if (repoUri is string repoUriStr)
                        try
                        {
                            RepoUri = new(repoUriStr);
                            if (RepoUri.Host == "github.com" && Version is not null)
                                UpdateCheck = new GitHubUpdateCheck(RepoUri, Version);
                        }
                        catch (Exception ex)
                        {
                            ModLoader.ModManager.OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
                        }

                if (modInfo.TryGetValue("mainModule", out var mainModule))
                    if (mainModule is string mainModuleStr)
                    {
                        MainModule = mainModuleStr;
                        MainModuleName = MainModule.EndsWith(".dll")
                            ? MainModule.Substring(0, MainModule.Length - 4)
                            : MainModule;
                    }

                if (modInfo.TryGetValue("net4Module", out var net4Module))
                    if (net4Module is string net4ModuleStr)
                    {
                        Net4Module = net4ModuleStr;
                        Net4ModuleName = Net4Module.EndsWith(".dll")
                            ? Net4Module.Substring(0, Net4Module.Length - 4)
                            : Net4Module;
                        if (Environment.Version.Major == 4)
                        {
                            MainModule = Net4Module;
                            MainModuleName = Net4ModuleName;
                        }
                    }

                if (modInfo.TryGetValue("net6Module", out var net6Module))
                    if (net6Module is string net6ModuleStr)
                    {
                        Net6Module = net6ModuleStr;
                        Net6ModuleName = Net6Module.EndsWith(".dll")
                            ? Net6Module.Substring(0, Net6Module.Length - 4)
                            : Net6Module;
                        if (Environment.Version.Major == 6)
                        {
                            MainModule = Net6Module;
                            MainModuleName = Net6ModuleName;
                        }
                    }

                if (modInfo.TryGetValue("mainClass", out var mainClass))
                    if (mainClass is string mainClassStr)
                        MainClass = mainClassStr;

                if (modInfo.TryGetValue("displayName", out var displayName))
                    if (displayName is string displayNameStr)
                        DisplayName = displayNameStr;

                if (modInfo.TryGetValue("loadPriority", out var loadPriority))
                    if (loadPriority is string loadPriorityStr)
                        if (double.TryParse(loadPriorityStr, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var loadPriorityVal))
                            LoadPriority = loadPriorityVal;

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

                if (modInfo.TryGetValue("generateManifest", out var genManifest))
                    if (genManifest is string genManifestStr)
                        ManifestGenerationType = genManifestStr;

                if (modInfo.TryGetValue("manifest", out var manifest))
                {
                    var dirUri = new Uri(Dir);
                    if (manifest is IDictionary<string, object> manifestObj)
                        foreach (var entry in manifestObj)
                        {
                            var filePathUri = new Uri(Path.Combine(Dir, entry.Key));
                            var relPath = dirUri.MakeRelativeUri(filePathUri).ToString();
                            if (relPath.StartsWith("../") || relPath.Contains("/../"))
                            {
                                Console.WriteLine($"Invalid manifest entry: {entry.Key}");
                                continue;
                            }
                            if (entry.Value is string hashStr)
                                _manifest[filePathUri.LocalPath] = hashStr;
                        }
                }

                if (modInfo.TryGetValue("overrideAssets", out var overrideAssets))
                    if (overrideAssets is string overrideAssetsStr)
                        OverrideAssets = overrideAssetsStr;

                if (modInfo.TryGetValue("patchedData", out var patchedData))
                    if (patchedData is string patchedDataStr)
                        PatchedData = patchedDataStr;

                Dependencies = depList.ToArray();
            }

            Console.WriteLine($"Parsed {this} from {dir}\\mod.json");

            if (IsValid)
                IsValid = ValidateManifest();
        }
        else
        {
            Console.WriteLine($"Missing {dir}\\mod.json");
            IsValid = false;
            Dependencies = Array.Empty<string>();
            Descriptor = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
        }
    }

    public Uri? RepoUri { get; }
    public double LoadPriority { get; }
    public bool WantsManifestGenerated => ManifestGenerationType is not null;
    public string? ManifestGenerationType { get; set; }
    public IReadOnlyDictionary<string, string> Manifest { get; }
    public string? OverrideAssets { get; }
    public string? PatchedData { get; }
    public string? Version { get; }
    public string? MainModuleName { get; }
    public string? Net4ModuleName { get; }
    public string? Net6ModuleName { get; }
    public bool IsValid { get; private set; }
    public IUpdateCheck? UpdateCheck { get; private set; }

    public IReadOnlyDictionary<string, object> Descriptor { get; }
    public IReadOnlyDictionary<string, IModInfo> ResolvedDependencies { get; }

    public Assembly? LoadedMainModule { get; private set; }

    public static IEnumerable<IModInfo> GetResolvedDependencies(IModInfo mod) => mod.ResolvedDependencies.Values;

    public bool ValidateManifest()
    {
        // buffer messages before sending them to console since we're running in parallel
        var msgBuf = new StringBuilder();

        var wantsManifest = WantsManifestGenerated;
        if (!_manifest.Any() && !wantsManifest)
        {
            msgBuf.AppendLine($"No manifest for {Name}, skipping validation");
            return true;
        }

        msgBuf.AppendLine(wantsManifest
            ? $"Generating and validating manifest for {Name}"
            : $"Validating manifest for {Name}");

        if (wantsManifest)
            foreach (var path in Directory.EnumerateFiles(Dir, "*", SearchOption.AllDirectories))
            {
                if (!_manifest.ContainsKey(path))
                    _manifest.Add(path, ManifestGenerationType + ":0x0");
            }

        var dirUri = new Uri(Dir + Path.DirectorySeparatorChar);
        foreach (var entry in _manifest)
        {
            var filePath = entry.Key;
            var hashStr = entry.Value;

            var hashStrParts = hashStr.Split(new[] { ':' }, 2);
            NonCryptographicHashAlgorithm? hasher = hashStrParts[0] switch
            {
                "Crc32" => new Crc32(),
                "Crc64" => new Crc64(),
                "XxHash32" => new XxHash32(),
                "XxHash64" => new XxHash64(),
                _ => null
            };

            if (hasher == null)
            {
                msgBuf.AppendLine($"Warning: skipping unsupported hash type {hashStrParts[0]}");
                continue;
            }

            DataUtils.ComputeFileHash(hasher, filePath);

            var checkStr = $"0x{hasher.GetCurrentHash().ToHexString()}";

            var relPath = dirUri.MakeRelativeUri(new(filePath)).ToString();

            if (wantsManifest && hashStrParts[1] == "0x0")
                msgBuf.AppendLine($"{relPath}: {hashStrParts[0]}:{checkStr}");

            else
            {
                if (hashStrParts[1] != checkStr)
                {
                    msgBuf.AppendLine($"{relPath}: {hashStr} FAIL -> {checkStr}");
                    return false;
                }

                msgBuf.AppendLine($"{relPath}: {hashStr} PASS");
            }
        }

        msgBuf.AppendLine();

        // print all the messages contiguously
        Console.WriteLine(msgBuf.ToString());

        return true;
    }

    public void ResolveDependencies()
    {
        foreach (var dep in Dependencies)
            if (ModLoader.ModManager.Mods.TryGetValue(dep, out var info))
                _resolvedDependencies[dep] = info;
    }

    public void Load(IServiceProvider sp)
    {
        Console.WriteLine($"Loading {this}");
        var dirName = Path.GetFileName(Dir);
        if (OverrideAssets is not null)
        {
            var overrideAssetsPath = Path.Combine("mods", dirName, OverrideAssets)
                .Replace('\\', '/');
            sp.GetService<IModManager>()!
                .OverrideAssetsStack
                .Push(overrideAssetsPath);
        }
        if (PatchedData is not null)
        {
            var patchedDataPath = Path.Combine("mods", dirName, PatchedData)
                .Replace('\\', '/');
            sp.GetService<IModManager>()!
                .PatchedDataStack
                .Push(patchedDataPath);
        }
    }
    public void InitializeModClass(IServiceProvider sp)
    {
        if (MainModule is null) return;
        var path = Path.Combine(Dir, MainModule);
        //UnblockUtil.UnblockDirectory(Dir);
        Console.WriteLine($"Loading module {MainModule} from {path}");
        var asm = ModManager.LoadAssembly(path);
        LoadedMainModule = asm;

        if (MainClass is null) return;
        var modType = asm.GetType(MainClass, false);
        if (modType is null)
        {
            Console.WriteLine($"Failed to load type: {MainClass}");
            IsValid = false;
            return;
        }

        _modType = modType;

        try
        {
            RuntimeHelpers.RunClassConstructor(modType.TypeHandle);
        }
        catch (Exception ex)
        {
            var edi = ExceptionDispatchInfo.Capture(ex);
            ModLoader.ModManager.OnUnhandledException(edi);
            Console.WriteLine($"Failed to initialize type: {MainClass}");
            IsValid = false;
        }

        if (modType.IsAbstract)
            _loadedMod = modType;
    }
    public void CreateModInstance(IServiceProvider sp)
    {
        if (MainModule is null) return;

        var modType = _modType;

        if (modType is null) return;

        if (modType.IsAbstract) return;

        try
        {
            _loadedMod = ActivatorUtilities.CreateInstance(sp, modType);
        }
        catch (Exception ex)
        {
            var edi = ExceptionDispatchInfo.Capture(ex);
            ModLoader.ModManager.OnUnhandledException(edi);
            Console.WriteLine($"Failed to create instance: {MainClass}");
            IsValid = false;
        }
    }

    public override string ToString()
    {

        if (!IsValid)
            return Path.GetFileName(Dir)!;

        var name = DisplayName ?? Name;

        var str = Version is not null
            ? $"{name} v{Version}"
            : name;

        if (UpdateCheck is not null && UpdateCheck.IsNewVersionAvailable)
            str += $"(Update Available: {UpdateCheck.NewVersion!.ToNormalizedString()})";

        return str;
    }

    public NonCryptographicHashAlgorithm GetHasher()
        => (NonCryptographicHashAlgorithm)Activator.CreateInstance(HasherType);

    public void UpdateHash()
        => _hash = DataUtils.GetDirectoryHash(GetHasher(), Dir);
    public byte[] Hash => _hash ??= DataUtils.GetDirectoryHash(GetHasher(), Dir);

    public string HashString => _hashString
        //??= $"{HasherType.Name}:{Convert.ToBase64String(Hash)}";
        ??= $"{HasherType.Name}:0x{Hash.ToHexString()}";

    public string ToString(bool extended)
    {
        var s = ToString();

        if (!extended) return s;

        if (LoadedMainModule is not null)
        {
            var verInfo = LoadedMainModule.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (verInfo.InformationalVersion != Version)
                s = $"{s}\n  - DLL v{verInfo.InformationalVersion}";
        }

        s = $"{s}\n  - {HashString}";

        return s;
    }
}
