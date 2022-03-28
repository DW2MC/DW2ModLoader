using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

public static class LanguageHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Rx([RegexPattern] string x) => x;
}
