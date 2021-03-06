using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using JetBrains.Annotations;
using MonoMod.Utils;
using NtApiDotNet;
using NtApiDotNet.Win32;

namespace DW2Net6Win.Isolation;

[PublicAPI]
public static class Windows
{
    public static bool IsProcessIsolated()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && IsProcessIsolatedInternal();

    private static bool IsProcessIsolatedInternal()
    {
        using var token = NtToken.OpenProcessToken();
        return token.AppContainer;
    }


    /// <summary>
    /// This job stays resident as held by this static member once created in order to terminate dependent child processes once the application exits.
    /// </summary>
    private static readonly Lazy<NtJob> CloseCascadeJob = new(() => {
        var job = NtJob.Create();
        job.SetLimitFlags(JobObjectLimitFlags.KillOnJobClose);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => job.Dispose();
        return job;
    });

    public delegate bool AceAccessCheck(Ace ace);

    public static event Action<ExceptionDispatchInfo>? UnhandledException;

    public static void OnUnhandledException(ExceptionDispatchInfo edi)
        => UnhandledException?.Invoke(edi);


    private static readonly unsafe delegate *managed<void*, Ace> ParseAce
        = (delegate *managed<void*, Ace>)typeof(Ace).GetMethod("Parse",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .GetLdftnPointer();

    /* This implements the AuthzAccessCheckCallback function.
     * Signature:
     *   BOOL CALLBACK AuthzAccessCheckCallback(
     *     _In_     AUTHZ_CLIENT_CONTEXT_HANDLE hAuthzClientContext,
     *     _In_     PACE_HEADER                 pAce,
     *     _In_opt_ PVOID                       pArgs,
     *     _Inout_  PBOOL                       pbAceApplicable
     *   )
     */
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall), typeof(CallConvSuppressGCTransition) })]
    private static unsafe int AuthzAccessCheckCallback(nuint hAuthzClientContext, void* pAce, nint pArgs, int* pbAceApplicable)
    {
        // use pArgs to map to context
        try
        {
            var ace = ParseAce(pAce);
            var ctx = GCHandle.FromIntPtr(pArgs);
            if (ctx.Target is AceAccessCheck fn)
            {
                *pbAceApplicable = fn(ace) ? 1 : 0;
                return 1;
            }
        }
        catch (Exception ex)
        {
            OnUnhandledException(ExceptionDispatchInfo.Capture(ex));
            return 0;
        }

        return 0; // unhandled
    }

    /// <summary>
    /// Starts the process isolated.
    /// </summary>
    /// <param name="appContainerName">This is the name of the AppContainer. Must be unique.</param>
    /// <param name="path">The executable path.</param>
    /// <param name="commandLineArguments">Any command line arguments.</param>
    /// <param name="capabilitySids">
    /// Additional capabilities such as network access the process should have.
    /// For private network access, try <see cref="KnownSids.CapabilityPrivateNetworkClientServer"/>.
    /// For public internet access, try <see cref="KnownSids.CapabilityInternetClient"/>.
    /// See <see cref="KnownSids"/> for more. </param>
    /// <param name="attachToCurrentProcess">
    /// If set to <c>true</c> the started process will be terminated when the current process exits.
    /// </param>
    /// <param name="fileAccess">
    /// The extended file access. Allows for custom file and directory access rights.
    /// You may prefer to specify only FileAccessRights.GenericRead for most paths. 
    /// </param>
    /// <param name="workingDirectory">
    /// The working directory of the process.
    /// </param>
    /// <returns>
    /// A process and an app container profile. They will need to be disposed of.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// "Couldn't resolve directory for &lt;application path&gt;."
    /// This exception occurs when the provided application path is not in a directory.
    /// </exception>
    [MustUseReturnValue]
    public static (Win32Process Process, AppContainerProfile Container) StartIsolatedProcess(
        string appContainerName,
        string path,
        string[]? commandLineArguments = null,
        IEnumerable<Sid>? capabilitySids = null,
        bool attachToCurrentProcess = true,
        IEnumerable<(string Path, FileSystemRights DirRights, FileSystemRights FileRights, bool Inherited)>? fileAccess = null,
        string? workingDirectory = null,
        bool lowPriv = false)
    {
        if (appContainerName is null) throw new ArgumentNullException(nameof(appContainerName));
        if (path is null) throw new ArgumentNullException(nameof(path));

        var applicationName = Path.GetFileNameWithoutExtension(path);

        var container = AppContainerProfile.Create(
            appContainerName,
            applicationName,
            $"AppContainer for {applicationName}");

        var config = new Win32ProcessConfig
        {
            ApplicationName = path,
            CommandLine = commandLineArguments is not null ? string.Join(" ", commandLineArguments) : string.Empty,
            ChildProcessMitigations = ChildProcessMitigationFlags.Restricted,
            AppContainerSid = container.Sid,
            TerminateOnDispose = attachToCurrentProcess,
            CurrentDirectory = workingDirectory is null ? null : Path.GetFullPath(workingDirectory),
            LowPrivilegeAppContainer = lowPriv,
            CreationFlags = attachToCurrentProcess ? CreateProcessFlags.None : CreateProcessFlags.BreakawayFromJob
        };

        // Apply file permissions
        if (fileAccess is not null)
            foreach (var cur in fileAccess)
            {
                if (!Directory.Exists(cur.Path) && !File.Exists(cur.Path))
                    throw new ArgumentException($"The path '{cur.Path}' does not exist.");

                AllowFileAccess(container, cur);
            }

        // Apply capabilities
        if (capabilitySids is not null)
            foreach (var capabilitySid in capabilitySids)
                config.AddCapability(capabilitySid);

        var process = Win32Process.CreateProcess(config);

        // Make sure the new process gets killed when the current process stops.
        if (attachToCurrentProcess)
            CloseCascadeJob.Value.AssignProcess(process.Process);

        return (process, container);
    }


    private static void AllowFileAccess(AppContainerProfile container,
        (string Path, FileSystemRights DirRights, FileSystemRights FileRights, bool Inherited) access)
        => AllowFileAccess(container, access.Path, access.DirRights, access.FileRights, access.Inherited);

    private static void AllowFileAccess(AppContainerProfile container, string path, FileSystemRights dirRights, FileSystemRights fileRights,
        bool inherited)
    {
        var sidBytes = container.Sid.ToArray();
        var sid = new SecurityIdentifier(sidBytes, 0);
        if (Directory.Exists(path))
        {
            var dir = new DirectoryInfo(path);
            var sec = dir.GetAccessControl(AccessControlSections.Access);
            var existing = sec.GetAccessRules(true, false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>()
                .ToArray();
            sec.PurgeAccessRules(sid);

            if (dirRights != default)
                sec.AddAccessRule(new(
                    sid,
                    dirRights,
                    InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                    inherited ? PropagationFlags.None : PropagationFlags.NoPropagateInherit,
                    AccessControlType.Allow
                ));

            if (fileRights != default)
                sec.AddAccessRule(new(
                    sid,
                    fileRights,
                    InheritanceFlags.ObjectInherit,
                    PropagationFlags.InheritOnly,
                    AccessControlType.Allow
                ));

            sec.SetAccessRuleProtection(false, false);

            try
            {
                dir.SetAccessControl(sec);
            }
            catch (UnauthorizedAccessException ex)
            {
                var sec2 = dir.GetAccessControl(AccessControlSections.Access);
                var existing2 = sec2.GetAccessRules(true, false, typeof(SecurityIdentifier))
                    .Cast<FileSystemAccessRule>()
                    .ToArray();
                foreach (var rule in existing2)
                {
                    if (rule.IdentityReference == sid) continue;
                    if (!existing.Contains(rule))
                        throw new SecurityException($"Unable to apply AppContainer restrictions to {path}", ex);
                }
                foreach (var rule in existing)
                {
                    if (rule.IdentityReference == sid) continue;
                    if (!existing2.Contains(rule))
                        throw new SecurityException($"Unable to apply AppContainer restrictions to {path}", ex);
                }
            }

            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
            {
                var fi = new FileInfo(file);
                var fs = fi.GetAccessControl(AccessControlSections.Access);
                if (!fs.AreAccessRulesProtected) continue;
                fs.PurgeAccessRules(sid);
                fs.SetAccessRuleProtection(false, false);
                try
                {
                    fi.SetAccessControl(fs);
                }
                catch (Exception ex)
                {
                    throw new SecurityException($"Unable to apply AppContainer restrictions to {file}", ex);
                }
            }

        }
        else if (File.Exists(path))
        {
            var file = new FileInfo(path);
            var sec = file.GetAccessControl(AccessControlSections.Access);
            var existing = sec.GetAccessRules(true, false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>()
                .ToArray();
            sec.PurgeAccessRules(sid);

            sec.AddAccessRule(new(
                sid,
                fileRights,
                InheritanceFlags.None,
                inherited ? PropagationFlags.None : PropagationFlags.NoPropagateInherit,
                AccessControlType.Allow
            ));

            sec.SetAccessRuleProtection(false, false);

            try
            {
                file.SetAccessControl(sec);
            }
            catch (UnauthorizedAccessException ex)
            {
                var sec2 = file.GetAccessControl(AccessControlSections.Access);
                var existing2 = sec2.GetAccessRules(true, false, typeof(SecurityIdentifier))
                    .Cast<FileSystemAccessRule>()
                    .ToArray();
                foreach (var rule in existing2)
                {
                    if (rule.IdentityReference == sid) continue;
                    if (!existing.Contains(rule))
                        throw new SecurityException($"Unable to apply AppContainer restrictions to {path}", ex);
                }
                foreach (var rule in existing)
                {
                    if (rule.IdentityReference == sid) continue;
                    if (!existing2.Contains(rule))
                        throw new SecurityException($"Unable to apply AppContainer restrictions to {path}", ex);
                }
            }
        }
    }

    [DllImport("advapi32", SetLastError = true)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern unsafe bool AdjustTokenPrivileges(
        SafeHandle TokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges = true,
        void* NewState = null,
        uint BufferLengthInBytes = 0,
        void* PreviousState = null,
        uint* ReturnLengthInBytes = null);

    internal static unsafe bool DisableProcessTokenPrivileges()
    {
        using var tok = NtToken.OpenProcessToken();
        return AdjustTokenPrivileges(tok.Handle);
    }

    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BringWindowToTop(nint hWnd);
}
