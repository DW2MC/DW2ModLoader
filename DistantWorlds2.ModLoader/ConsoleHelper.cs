using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

using static ConsoleHelper.NativeMethods;

internal enum ControlHandlerEventType : uint
{
    CtrlC = 0,
    CtrlBreak = 1,
    Close = 2,
    LogOff = 5,
    ShutDown = 6
}

[PublicAPI]
internal static class ConsoleHelper
{
    public static void CreateConsole()
    {
        if (GetConsoleWindow() != default)
            return;

        AllocConsole();

        SetConsoleCtrlHandler(HandlerRoutine, true);
    }

    public static ConsoleCtrlHandlerRoutine HandlerRoutine = OnConsoleControlEvent;

    public static event Func<ControlHandlerEventType, bool>? ConsoleControlEvent;

    private static bool OnConsoleControlEvent(ControlHandlerEventType type)
        => ConsoleControlEvent?.Invoke(type) ?? false;

    public static bool TryEnableVirtualTerminalProcessing()
    {
        try
        {
            var stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            GetConsoleMode(stdHandle, out var mode);
            SetConsoleMode(stdHandle, mode | 4);
            GetConsoleMode(stdHandle, out mode);
            return (mode & 4) == 4;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool _freedConsole;

    public static bool IsConsoleActive => !_freedConsole;
    public static void TryDetachFromConsoleWindow()
    {
        if (GetConsoleWindow() == default)
            return;

        _freedConsole = FreeConsole();

        SetConsoleCtrlHandler(HandlerRoutine, false);
        
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);
    }

    internal static class NativeMethods
    {
        // ReSharper disable InconsistentNaming
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint OPEN_EXISTING = 3;
        internal const uint STD_OUTPUT_HANDLE = unchecked((uint)-11);
        internal const uint STD_ERROR_HANDLE = unchecked((uint)-12);
        // ReSharper restore InconsistentNaming

        private const string Kernel32 = "kernel32";

        [DllImport(Kernel32, SetLastError = true)]
        internal static extern void AllocConsole();

        [DllImport(Kernel32, SetLastError = true)]
        internal static extern nuint CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            nint pAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            nuint hTemplateFile);

        [DllImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetStdHandle(
            uint nStdHandle,
            nuint hHandle);

        [DllImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetConsoleMode(nuint hConsoleHandle, uint dwMode);

        [DllImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetConsoleMode(nuint hConsoleHandle, out uint lpMode);

        [DllImport(Kernel32, SetLastError = true)]
        internal static extern nuint GetStdHandle(uint handle);

        [DllImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeConsole();

        [DllImport(Kernel32, SetLastError = true)]
        internal static extern nuint GetConsoleWindow();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool ConsoleCtrlHandlerRoutine(ControlHandlerEventType ctrlType);

        [DllImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool SetConsoleCtrlHandler(
            [MarshalAs(UnmanagedType.FunctionPtr)] ConsoleCtrlHandlerRoutine pHandlerRoutine,
            [MarshalAs(UnmanagedType.Bool)] bool bAdd);
    }
}
