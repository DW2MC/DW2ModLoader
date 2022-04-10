using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using DistantWorlds2.ModLoader;
using static System.Reflection.BindingFlags;

namespace CommunityPatch
{
    public static class CallStackHelpers
    {
        private static readonly unsafe delegate *<int> PFnGetCallStackDepth;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetCallStackDepth() => PFnGetCallStackDepth();


        public static bool _debuggerIsAware = false;

        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
        private static readonly AssemblyBuilder DynAsm;

        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
        private static readonly ModuleBuilder DynMod;

        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
        private static readonly TypeBuilder DynType;


        static CallStackHelpers()
        {
            while (Debugger.IsAttached && !_debuggerIsAware)
                Debugger.Break();

            var stAsm = typeof(StackTrace).Assembly;
            var coreAsm = typeof(object).Assembly;
            var stackFrameHelperType = stAsm.GetType("System.Diagnostics.StackFrameHelper", false)
                ?? coreAsm.GetType("System.Diagnostics.StackFrameHelper", false);

            var getStackFramesInternal = typeof(StackTrace).GetMethod("GetStackFramesInternal", Static | NonPublic);

            Debugger.Log(0, "CallStackHelpers", $"GetStackFramesInternal? {getStackFramesInternal is not null}\n");

            var iFrameCountField = stackFrameHelperType
                .GetField("iFrameCount", NonPublic | Instance | DeclaredOnly);

            var ignoreAttrCtor = ReflectionUtils.Constructor(() => new IgnoresAccessChecksToAttribute(null!));

            DynAsm = AssemblyBuilder.DefineDynamicAssembly(new("CallStackHelpers"), AssemblyBuilderAccess.Run);
            var stAsmName = stAsm.GetName().Name;
            var coreAsmName = coreAsm.GetName().Name;
            var caIgnore1 = new CustomAttributeBuilder(ignoreAttrCtor, new object?[] { stAsmName });
            var caIgnore2 = new CustomAttributeBuilder(ignoreAttrCtor, new object?[] { coreAsmName });
            DynAsm.SetCustomAttribute(caIgnore1);
            DynAsm.SetCustomAttribute(caIgnore2);

            DynMod = DynAsm.DefineDynamicModule("CallStackHelpers");

            Debugger.Log(0, "CallStackHelpers", $"iFrameCount? {iFrameCountField is not null}\n");

            DynType = DynMod.DefineType("CallStackHelpers",
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);

            var dynMethod = DynType.DefineMethod("GetStackFrameCount", MethodAttributes.Public | MethodAttributes.Static, typeof(int),
                Type.EmptyTypes);

            var constructorInfo = stackFrameHelperType.GetConstructor(new[] { typeof(Thread) });

            Debugger.Log(0, "CallStackHelpers", $"StackFrameHelper.ctor? {constructorInfo is not null}\n");

            {
                var il = dynMethod.GetILGenerator();
                il.DeclareLocal(stackFrameHelperType);

                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Newobj, constructorInfo!);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldnull);
                il.EmitCall(OpCodes.Call, getStackFramesInternal!, null);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldfld, iFrameCountField!);
                il.Emit(OpCodes.Ret);
            }

            var t = DynType.CreateTypeInfo()!;

            var m = t.GetMethod("GetStackFrameCount")!;

            unsafe { PFnGetCallStackDepth = (delegate*<int>)m.MethodHandle.GetFunctionPointer(); }
        }

        public static void Init()
        {
            // static constructor will init
        }
    }
}
