using System;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace DistantWorlds2.ModLoader;

using static ConsoleHelper.NativeMethods;

[PublicAPI]
internal static class ConsoleHelper
{
    public static void CreateConsole()
    {
        AllocConsole();

        var hConOut = CreateFile(
            @"CONOUT$",
            GENERIC_WRITE,
            0,
            0,
            OPEN_EXISTING,
            0,
            0
        );

        SetStdHandle(STD_OUTPUT_HANDLE, hConOut);
        SetStdHandle(STD_ERROR_HANDLE, hConOut);
    }

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
        if (GetConsoleWindow() == default
            || Console.IsOutputRedirected
            || Console.IsErrorRedirected
            || Console.IsInputRedirected)
            return;

        _freedConsole = FreeConsole();
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
    }
}
