using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Alkawa.Engine;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace LostCrownAchievements;

[BepInPlugin("local.lostcrown.achievementmenu", "Achievement Menu", "1.1.0")]
public sealed class Plugin : BasePlugin
{
    private static AchievementMenu menu;
    internal static BepInEx.Logging.ManualLogSource MenuLog;
    public override void Load()
    {
        MenuLog = Log;
        menu = new AchievementMenu(Path.GetDirectoryName(typeof(Plugin).Assembly.Location));
        new Harmony("local.lostcrown.achievementmenu").Patch(
            AccessTools.Method(typeof(UbiconnectPCManager), "Update"),
            postfix: new HarmonyMethod(typeof(Plugin), nameof(Pulse)));
        Log.LogInfo("Achievement menu loaded. Press F8 to open its Windows window.");
    }
    private static void Pulse() => menu.Tick();
}

internal sealed class AchievementMenu
{
    private readonly string folder;
    private readonly List<Entry> entries = new();
    private int platform = 1;
    private int selected = -1;
    private string listId = Guid.NewGuid().ToString();
    private string status = "Select a platform and refresh.";
    private Func<bool> pending;
    private long deadline;
    private long nextPoll;
    private bool keyWasDown;
    private bool pulseLogged;
    private Process window;

    internal AchievementMenu(string folder) { this.folder = folder; Publish(); }
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);

    internal void Tick()
    {
        try
        {
            if (!pulseLogged) { Plugin.MenuLog.LogInfo("Achievement menu update hook is active."); pulseLogged = true; }
            var keyDown = (GetAsyncKeyState(0x77) & 0x8000) != 0;
            GetWindowThreadProcessId(GetForegroundWindow(), out var foreground);
            if (keyDown && !keyWasDown && foreground == Environment.ProcessId)
            {
                if (window == null || window.HasExited)
                {
                    window = Process.Start(new ProcessStartInfo("dotnet", "\"" + Path.Combine(folder, "MenuApp.dll") + "\" \"" + folder + "\"")
                    { UseShellExecute = false, CreateNoWindow = true });
                    Plugin.MenuLog.LogInfo("Opened achievement menu window.");
                }
            }
            keyWasDown = keyDown;
            if (Environment.TickCount64 < nextPoll) return;
            nextPoll = Environment.TickCount64 + 100;
            var commandPath = Path.Combine(folder, "command.json");
            if (File.Exists(commandPath))
            {
                var json = File.ReadAllText(commandPath);
                File.Delete(commandPath);
                var command = JsonSerializer.Deserialize<Command>(json);
                if (command == null || command.Platform != 1) throw new InvalidOperationException("Only Ubisoft Connect requests are supported.");
                if (pending != null) throw new InvalidOperationException("Another achievement request is still pending.");
                if (command.Index == -1) { platform = command.Platform; Refresh(); }
                else
                {
                    if (!Selection.Valid(command, CurrentView())) throw new InvalidOperationException("Selection is stale. Refresh and select again.");
                    selected = command.Index;
                    UnlockSelected();
                }
                Publish();
            }
            if (pending != null)
            {
                if (pending()) { pending = null; Publish(); }
                else if (Environment.TickCount64 > deadline)
                {
                    pending = null;
                    status = "Response timed out. Refresh before retrying.";
                    Publish();
                }
            }
        }
        catch (IOException) { }
        catch (Exception ex)
        {
            Fail(ex);
            try { Publish(); } catch (IOException publishError) { Plugin.MenuLog.LogError(publishError.Message); }
        }
    }

    private View CurrentView() => new(listId, platform, entries.ToArray(), status, pending != null);
    private void Publish()
    {
        File.WriteAllText(Path.Combine(folder, "view.tmp"), JsonSerializer.Serialize(CurrentView()));
        File.Move(Path.Combine(folder, "view.tmp"), Path.Combine(folder, "view.json"), true);
    }
    private void WaitFor(Func<bool> poll) { pending = poll; deadline = Environment.TickCount64 + 20000; }
    private void Fail(Exception ex) { pending = null; status = ex.Message; Plugin.MenuLog.LogError(ex); }
    private static IntPtr UbisoftContext()
    {
        if (!UbiconnectPCManager.Initialized || UbiconnectPCManager.UPCContext == IntPtr.Zero)
            throw new InvalidOperationException("Ubisoft Connect is not ready. Sign in and load the main menu.");
        return UbiconnectPCManager.UPCContext;
    }

    private void Refresh()
    {
        if (pending != null) return;
        listId = Guid.NewGuid().ToString();
        entries.Clear();
        selected = -1;
        try
        {
                var task = UplayPCWrapper.UPC_AchievementListGet(UbisoftContext(), UbiconnectPCManager.UserId, 0, null);
                status = "Loading Ubisoft Connect achievements...";
                WaitFor(() =>
                {
                    if (!task.IsCompleted) return false;
                    if (task.IsFaulted || task.IsCanceled) throw new InvalidOperationException("Ubisoft achievement list request failed.");
                    var result = task.Result;
                    if ((int)result.Result != 0) throw new InvalidOperationException("Ubisoft error: " + result.Result);
                    foreach (var a in result.ResultData)
                        entries.Add(new Entry(a.nameUtf8, a.descriptionUtf8, "", a.id, a.completed != 0));
                    entries.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name));
                    status = $"{entries.Count} Ubisoft Connect achievements.";
                    return true;
                });
        }
        catch (Exception ex) { Fail(ex); }
    }

    private void UnlockSelected()
    {
        if (pending != null || selected < 0 || selected >= entries.Count) return;
        var entry = entries[selected];
        if (entry.Unlocked) return;
        try
        {
                var task = UplayPCWrapper.UPC_AchievementUnlock(UbisoftContext(), entry.UbisoftId, null);
                status = "Requesting Ubisoft Connect unlock...";
                WaitFor(() =>
                {
                    if (!task.IsCompleted) return false;
                    if (task.IsFaulted || task.IsCanceled) throw new InvalidOperationException("Ubisoft unlock request failed.");
                    if ((int)task.Result.Result != 0) throw new InvalidOperationException("Ubisoft error: " + task.Result.Result);
                    entries[selected] = entry with { Unlocked = true };
                    listId = Guid.NewGuid().ToString();
                    status = "Ubisoft Connect accepted the unlock. Refresh to verify.";
                    return true;
                });
        }
        catch (Exception ex) { Fail(ex); }
    }



}

