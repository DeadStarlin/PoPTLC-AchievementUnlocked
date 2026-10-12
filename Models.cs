using System;

namespace LostCrownAchievements;

public sealed record Entry(string Name, string Description, string SteamId, uint UbisoftId, bool Unlocked);
public sealed record View(string ListId, int Platform, Entry[] Entries, string Status, bool Busy);
public sealed record Command(string RequestId, int Platform, int Index, string ListId);

internal static class Selection
{
    internal static bool ListChanged(View previous, View next) => previous?.ListId != next.ListId ||
        previous?.Entries.Length != next.Entries.Length;
    internal static bool Valid(Command command, View view) => command.Platform == view.Platform &&
        command.ListId == view.ListId && command.Index >= 0 && command.Index < view.Entries.Length &&
        !view.Busy && !view.Entries[command.Index].Unlocked;
}
