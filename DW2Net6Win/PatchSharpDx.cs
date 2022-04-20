/* Included in v1.0.3.3
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FastExpressionCompiler.LightExpression;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using DeviceChild = SharpDX.Direct3D11.DeviceChild;

namespace DW2Net6Win;

public static class PatchSharpDx
{
    private static MethodInfo Method(Expression<Action> a)
    {
        var body = a.Body;
        return body is MethodCallExpression mce
            ? mce.Method
            : throw new MissingMemberException("No method");
    }

    public static void ApplyIfNeeded()
    {

        var needsDeviceLocks = Environment.GetEnvironmentVariable("DW2MC_DX11_DEVICE_LOCKS") == "1";
        if (needsDeviceLocks)
            Console.WriteLine("Explicitly adding DX11 device locks.");
        else
        {
            try
            {
                var fac = new Factory1();
                var adapter = fac.GetAdapter(0);
                var desc = adapter.Description;
                if (desc.VendorId is 0x1002 or 0x1022)
                    needsDeviceLocks = true;
                var descStr = desc.Description;
                if (needsDeviceLocks == false
                    && (descStr.Contains("AMD ", StringComparison.OrdinalIgnoreCase)
                        || descStr.Contains("ATi ", StringComparison.OrdinalIgnoreCase)))
                    needsDeviceLocks = true;
                Console.WriteLine($"GPU: {descStr}");
            }
            catch
            {
                // oh well
            }

            if (Environment.GetEnvironmentVariable("DW2MC_DX11_DEVICE_LOCKS") == "0")
            {
                Console.WriteLine("Explicitly not adding DX11 device locks.");
                needsDeviceLocks = false;
            }
        }

        if (needsDeviceLocks)
            Apply();
    }
    public static void Apply()
    {
        var prefix = new HarmonyMethod(Method(() => PrefixEnterLockInstance(null!)));
        var postfix = new HarmonyMethod(Method(() => PostfixExitLockInstance(null!)));
        var types = new[]
        {
            typeof(Device),
            typeof(SharpDX.Direct3D11.Device1),
            typeof(SharpDX.Direct3D11.Device2),
            typeof(SharpDX.Direct3D11.Device3),
            typeof(SharpDX.Direct3D11.Device4),
            typeof(Device5),
            typeof(DeviceContext),
            typeof(DeviceContext1),
            typeof(DeviceContext2),
            typeof(DeviceContext3),
            typeof(DeviceContext4),
            typeof(Device11On12),
            typeof(DeviceChild)
        };
        foreach (var t in types)
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (m.IsStatic || m.IsConstructor || m.IsGenericMethod || m.IsAbstract || m.IsVirtual) continue;
            try { Program.Harmony.Patch(m, prefix, postfix); }
            catch
            {
                /* oh no! anyway... *-/
            }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
    public static bool PrefixEnterLockInstance(object __instance)
    {
        Monitor.Enter(__instance);
        return true;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void PostfixExitLockInstance(object __instance)
        => Monitor.Exit(__instance);
}
*/