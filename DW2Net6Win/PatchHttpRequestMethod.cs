using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using HarmonyLib;
using JetBrains.Annotations;

[PublicAPI]
[HarmonyPatch(typeof(HttpRequestMessage))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public static class PatchHttpRequestMethod
{
    [HarmonyPrefix]
    [HarmonyPatch("DefaultRequestVersion")]
    [HarmonyPatch(MethodType.Getter)]
    public static bool DefaultRequestVersion(ref Version __result)
    {
        __result = HttpVersion.Version11;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("DefaultVersionPolicy")]
    [HarmonyPatch(MethodType.Getter)]
    public static bool DefaultVersionPolicy(ref HttpVersionPolicy __result)
    {
        __result = HttpVersionPolicy.RequestVersionOrHigher;
        return false;
    }
}
