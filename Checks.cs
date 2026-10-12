using System;
using System.Text.Json;

namespace LostCrownAchievements;

internal static class Checks
{
    public static void Main()
    {
        var view = new View("current", 0, new[] { new Entry("Test", "", "test_id", 1, false) }, "Ready", false);
        var command = new Command("request", 0, 0, "current");
        if (!Selection.Valid(command, view) ||
            Selection.Valid(command with { Platform = 1 }, view) ||
            Selection.Valid(command with { ListId = "stale" }, view) ||
            Selection.Valid(command with { Index = -1 }, view) ||
            Selection.Valid(command with { Index = 1 }, view) ||
            Selection.Valid(command, view with { Busy = true }) ||
            Selection.Valid(command, view with { Entries = new[] { view.Entries[0] with { Unlocked = true } } }))
            throw new Exception("Achievement selection validation failed.");
        var roundTrip = JsonSerializer.Deserialize<View>(JsonSerializer.Serialize(view));
        if (roundTrip == null || !Selection.Valid(command, roundTrip) || roundTrip.Entries[0].SteamId != "test_id")
            throw new Exception("Menu protocol round-trip failed.");
        var loading = view with { Entries = Array.Empty<Entry>(), Busy = true };
        if (!Selection.ListChanged(loading, view) || Selection.ListChanged(view, view) || !Selection.ListChanged(null, view))
            throw new Exception("Async list completion did not trigger a redraw.");
        Console.WriteLine("Passed: selection validation, JSON round-trip, async list redraw.");
    }
}
