using System.Reflection;

namespace DistantWorlds2.ModLoader;

public interface IModInfo
{
    string Name { get; }

    string Dir { get; }

    string[] Dependencies { get; }

    string? MainModule { get; }

    string? Net4Module { get; }

    string? Net6Module { get; }

    string? MainClass { get; }

    string? DisplayName { get; }

    Uri? RepoUri { get; }

    double LoadPriority { get; }

    bool WantsManifestGenerated { get; }

    string? ManifestGenerationType { get; set; }

    IReadOnlyDictionary<string, string> Manifest { get; }

    string? OverrideAssets { get; }

    string? PatchedData { get; }

    string? Version { get; }

    string? MainModuleName { get; }

    string? Net4ModuleName { get; }

    string? Net6ModuleName { get; }

    bool IsValid { get; }

    IUpdateCheck? UpdateCheck { get; }

    IReadOnlyDictionary<string, object> Descriptor { get; }

    IReadOnlyDictionary<string, IModInfo> ResolvedDependencies { get; }

    Assembly? LoadedMainModule { get; }

    byte[] Hash { get; }

    string HashString { get; }

    void ResolveDependencies();

    void Load(IServiceProvider sp);

    bool ValidateManifest();

    void UpdateHash();

    string ToString(bool extended);
}
