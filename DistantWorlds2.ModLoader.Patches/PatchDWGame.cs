using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DistantWorlds.Types;
using DistantWorlds.UI;
using DistantWorlds2;
using DistantWorlds2.ModLoader;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;
using Xenko.Core.LZ4;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering;

[PublicAPI]
[HarmonyPatch(typeof(DWGame))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public static class PatchGameSaveLoad
{
    [StructLayout(LayoutKind.Sequential)]
    struct LZ4Header
    {
        public const uint LZ4_SIGNATURE = 0x184D2204;
        public uint MagicNb;
        //public byte[] FrameDescriptor; Supposed to be part of LZ4 header, but unused by Xenko.
    };

    [HarmonyPatch(typeof(DWGame), nameof(DWGame.SaveGame))]
    [HarmonyPrefix]
    public static bool PrefixSaveGame(DWGame __instance, ScaledRenderer ____Renderer, Galaxy ____Galaxy, string filePath)
    {
        __instance.PauseGame(true);

        if(!ModLoader.IsIsolated)
        {
            ____Renderer.CheckSaveSpace();
        }

        GameGalaxyData gameGalaxyData = new GameGalaxyData();
        __instance.Player.ExtractTimeValues(out gameGalaxyData.StartTimeTicks, out gameGalaxyData.LastServerTimeTicks, out gameGalaxyData.LocalTimeAtLastServerTimeTicks, out gameGalaxyData.StopWatchStartTimeStamp, out gameGalaxyData.StopWatchElapsed);
        ____Renderer.GetViewData(out gameGalaxyData.SceneRenderType, out gameGalaxyData.ViewPosition, out gameGalaxyData.ViewForward, out gameGalaxyData.ViewUp, out gameGalaxyData.GalaxyX, out gameGalaxyData.GalaxyY);
        try
        {
            using (Stream output = VirtualFileSystem.ApplicationData.OpenStream(filePath, VirtualFileMode.Create, VirtualFileAccess.ReadWrite))
            {                
                if (PatchFileDialog._compressSave)
                {
                    using var metaWriter = new BinaryWriter(output);
                    metaWriter.Write(LZ4Header.LZ4_SIGNATURE);
                    metaWriter.Flush();
                    using LZ4Stream lZ4Stream = new LZ4Stream(output, CompressionMode.Compress, true);
                    using (BinaryWriter writer = new BinaryWriter(lZ4Stream))
                        ____Galaxy.WriteToStream(writer, gameGalaxyData);
                }
                else
                {
                    using (BinaryWriter writer = new BinaryWriter(output))
                        ____Galaxy.WriteToStream(writer, gameGalaxyData);
                }
            }
            if (__instance.SaveMessageLog)
            {
                using (Stream stream = VirtualFileSystem.ApplicationData.OpenStream("MessageLog.xml", VirtualFileMode.Create, VirtualFileAccess.ReadWrite))
                    EmpireMessageList.XmlSerializer.WriteXml(stream, ____Galaxy.Empires.GetPlayer().Messages);
                __instance.SaveMessageLog = false;
            }
        }
        catch (Exception ex)
        {
            const int HR_ERROR_HANDLE_DISK_FULL = unchecked((int)0x80070027);
            const int HR_ERROR_DISK_FULL = unchecked((int)0x80070070);

            TextHelper.SessionLog("Error while saving game");
            TextHelper.SessionLog(ex.ToString());
            ____Renderer.SetBriefMessage(TextResolver.GetText("Error while saving game. Check that you have enough storage space"), 15f);

            if (ex.HResult == HR_ERROR_DISK_FULL || ex.HResult == HR_ERROR_HANDLE_DISK_FULL)
            {
                Vector2 size = UserInterfaceHelper.CalculateScaledValue(new Vector2(500f, 250f));
                UserInterfaceController.ShowMessageDialogCentered(null, null, ImageFill.Zoom, "ERROR", "Error - Failed to save game. Disk Full!", true,
                    new DWButtonData(string.Empty, TextResolver.GetText("OK"), null, null),
                    null, UserInterfaceController.ScreenWidth, UserInterfaceController.ScreenHeight, size);
                UserInterfaceController.MessageDialog.Layer = 32000f;
            }
            else
            {
                DWGame.DoCrashDump(ex);
            }
        }
        Galaxy.AllowNewTasks = true;
        return false;
    }

    [HarmonyPatch(typeof(DWGame), nameof(DWGame.LoadGame))]
    [HarmonyPrefix]
    public static bool LoadGamePrefix(DWGame __instance, string filePath)
    {
        Galaxy.AllowNewTasks = false;
        Galaxy.LoadSensitiveRenderingPaused = true;
        Galaxy galaxy = new Galaxy();
        GameGalaxyData gameGalaxyData;
        using (Stream input = VirtualFileSystem.ApplicationData.OpenStream(filePath, VirtualFileMode.Open, VirtualFileAccess.Read))
        {
            using BinaryReader metaReader = new BinaryReader(input);
            if (metaReader.ReadUInt32() == LZ4Header.LZ4_SIGNATURE)
            {
                using LZ4Stream lZ4Stream = new LZ4Stream(input, CompressionMode.Decompress, true);
                using (BinaryReader reader = new BinaryReader(lZ4Stream))
                    galaxy.ReadFromStream(reader, out gameGalaxyData);
            }
            else
            {
                input.Seek(0, SeekOrigin.Begin);
                using (BinaryReader reader = new BinaryReader(input))
                    galaxy.ReadFromStream(reader, out gameGalaxyData);
            }
        }
        PathFindingSystem.CalculateSystemDistances(galaxy);
        galaxy.ApplyDifficultySetting(galaxy.GameStartSettings.Difficulty, galaxy.GameStartSettings.DifficultyScaling);
        __instance.PauseGame(true);
        __instance.CheckInitializeOrBindSinglePlayerGame(galaxy, galaxy.Empires.GetPlayer(), galaxy.Time);
        galaxy.GameServer = __instance.Server;
        galaxy.Time = __instance.GetServerTime();
        galaxy.ProcessPostLoad(__instance.DWSettings);
        __instance.DisableDrawing();
        galaxy._ShipHullModelDataLoaded = false;
        galaxy.LoadShipHullModelData(__instance, __instance.Content, __instance.GraphicsDevice);
        galaxy.LoadImageForAllItems(__instance, __instance.Content);
        Galaxy.VersionStatic = DWGame.Version;
        __instance.StartGameExisting(galaxy, galaxy.Time, gameGalaxyData);
        Galaxy.LoadSensitiveRenderingPaused = false;
        Galaxy.AllowNewTasks = true;
        __instance.EnableDrawing();
        return false;
    }
}

[PublicAPI]
[HarmonyPatch(typeof(FileDialog))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "RedundantAssignment")]
public static class PatchFileDialog
{
    private static ConditionalWeakTable<FileDialog, DWButton> _saveCompressedButtons = new ConditionalWeakTable<FileDialog, DWButton>();
    private static bool _initialized = false;
    public static bool _compressSave = true;
    [HarmonyPatch(typeof(FileDialog), nameof(FileDialog.ShowCentered))]
    [HarmonyPrefix]
    public static bool ShowCenteredPrefix(FileDialog __instance)
    {
        DWButton? compressButton = null;
        if (!_initialized)
        {
            Vector2 size = UserInterfaceHelper.CalculateScaledValue(new Vector2(__instance.ButtonHeight, __instance.ButtonHeight));
            compressButton = new DWButton();            
            compressButton.Name = "CompressButton";
            compressButton.ButtonColor = ColorHelper.ButtonColor;
            compressButton.ForeColorHover = ColorHelper.ButtonColorTextHover;
            compressButton.ButtonColorHover = ColorHelper.ButtonColorHover;
            compressButton.ImageSprite = null;
            compressButton.UseBoldFont = true;
            compressButton.Text = "Save Compressed";

            __instance.DeleteButton.Size -= new Vector2(size.X + __instance.Margin + __instance.HalfLineHeight, 0.0f);
            compressButton.Position = __instance.DeleteButton.Position + new Vector2(__instance.DeleteButton.Size.X + __instance.Margin + __instance.HalfLineHeight, 0.0f);
            compressButton.TargetPosition = compressButton.Position;
            compressButton.Size = size;
            compressButton.TargetSize = size;
            compressButton.SetSizeAndPosition(size, compressButton.Position);
            compressButton.Initialize(FontSize.Normal, FontSize.Large, null);
            compressButton.ShowDropDownSelector = false;            

            __instance.AddControl(compressButton);
            _saveCompressedButtons.Add(__instance, compressButton);
            _initialized = true;
        }
        
        if (_saveCompressedButtons.TryGetValue(__instance, out compressButton) && __instance.HeaderText == TextResolver.GetText("Save Game").ToUpperInvariant())
        {
            compressButton.Visible = true;
            var saveHandler = __instance.OkButton.ClickEvent;
            compressButton.PreClickEvent = __instance.OkButton.PreClickEvent;
            compressButton.ClickEvent = new EventHandler<DWEventArgs>((sender, args) =>
            {
                _compressSave = true;
                args.ExtraData = __instance.OkButton.ExtraClickData;
                saveHandler(sender, args);
            });

            __instance.OkButton.ClickEvent = new EventHandler<DWEventArgs>((sender, args) =>
            {
                _compressSave = false;
                saveHandler(sender, args);
            });
        }
        else
            compressButton.Visible = false;

        return true;
    }

    [HarmonyPatch(typeof(FileDialog), nameof(FileDialog.Render), typeof(RenderDrawContext), typeof(SpriteBatch), typeof(Point), typeof(bool))]
    [HarmonyPostfix]
    public static void RenderPostfix(FileDialog __instance)
    {
        DWButton compressButton;
        if (!_saveCompressedButtons.TryGetValue(__instance, out compressButton))
            return;

        int button_count = new[] { __instance.OkButton.Visible, __instance.CancelButton.Visible, __instance.DeleteButton.Visible, compressButton.Visible }.Count(x => x);

        float available_width = __instance.Size.X - __instance.Margin * 2f;
        float button_width = (available_width - __instance.HalfLineHeight * button_count) / button_count;

        float x = __instance.Margin;
        if (compressButton.Visible && __instance.OkButton != null)
        {
            compressButton.Position = new Vector2(x, __instance.OkButton.Position.Y);
            compressButton.TargetPosition = compressButton.Position;
            compressButton.Size = new Vector2(button_width, __instance.ButtonHeight);
            compressButton.TargetSize = compressButton.Size;
            x += compressButton.Size.X + __instance.Margin;
        }
        if (__instance.OkButton != null)
        {
            __instance.OkButton.Position.X = x;
            __instance.OkButton.TargetPosition = __instance.OkButton.Position;
            __instance.OkButton.Size = new Vector2(button_width, __instance.ButtonHeight);
            __instance.OkButton.TargetSize = __instance.OkButton.Size;

            x += __instance.OkButton.Size.X + __instance.Margin;
        }
        if (__instance.CancelButton != null)
        {
            __instance.CancelButton.Position.X = x;
            __instance.CancelButton.TargetPosition = __instance.CancelButton.Position;
            __instance.CancelButton.Size = new Vector2(button_width, __instance.ButtonHeight);
            __instance.CancelButton.TargetSize = __instance.CancelButton.Size;
            x += __instance.CancelButton.Size.X + __instance.Margin;
        }
        if (__instance.DeleteButton != null && __instance.DeleteButton.Visible)
        {
            __instance.DeleteButton.Position.X = x;
            __instance.DeleteButton.TargetPosition = __instance.DeleteButton.Position;
            __instance.DeleteButton.Size = new Vector2(button_width, __instance.ButtonHeight);
            __instance.DeleteButton.TargetSize = __instance.DeleteButton.Size;
            x += __instance.DeleteButton.Size.X + __instance.Margin;
        }
    }
}

/* maybe just disable MessageLog.xml?
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DistantWorlds.Types;
using HarmonyLib;
using JetBrains.Annotations;
using Xenko.Core.IO;

namespace DistantWorlds2.ModLoader;

[PublicAPI]
[HarmonyPatch(typeof(DWGame))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PatchDWGame
{
    [HarmonyPatch(nameof(DWGame.SaveGame))]
    [HarmonyPrefix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PrefixSaveGame(DWGame __instance, out bool __state)
    {
        if (!ModLoader.IsIsolated)
        {
            __state = false;
            return true;
        }

        __state = __instance.SaveMessageLog;
        __instance.SaveMessageLog = false;
        return true;
    }

    [HarmonyPatch(nameof(DWGame.SaveGame))]
    [HarmonyPostfix]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PostfixSaveGame(DWGame __instance, bool __state, ref Galaxy ____Galaxy)
    {
        if (!__state) return;

        using var s = VirtualFileSystem.ApplicationLocal.OpenStream("MessageLog.xml", VirtualFileMode.Create, VirtualFileAccess.ReadWrite);
        EmpireMessageList.XmlSerializer.WriteXml(s, ____Galaxy.Empires.GetPlayer().Messages);
    }
}
*/