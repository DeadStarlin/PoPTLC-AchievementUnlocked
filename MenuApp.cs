using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;

namespace LostCrownAchievements;

internal static class MenuApp
{
    [STAThread]
    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MenuForm(args[0]));
    }
}

internal sealed class MenuForm : Form
{
    private readonly string folder;
    private View view;
    private string lastJson = "";
    private readonly List<int> visibleIndices = new();
    private readonly ListBox list = new() { Left = 12, Top = 52, Width = 650, Height = 380 };
    private readonly Label description = new() { Left = 12, Top = 440, Width = 650, Height = 65 };
    private readonly Label status = new() { Left = 12, Top = 550, Width = 650, Height = 60 };
    private readonly Button unlock = new() { Left = 12, Top = 510, Width = 300, Text = "Unlock selected achievement", Enabled = false };
    private readonly Label platformLabel = new() { Left = 12, Top = 16, Width = 300, Text = "Ubisoft Connect achievements" };
    private readonly Button refresh = new() { Left = 510, Top = 12, Width = 150, Text = "Refresh" };
    private readonly Timer timer = new() { Interval = 250 };

    private int SelectedEntryIndex
    {
        get
        {
            if (view == null || list.SelectedIndex < 0 || list.SelectedIndex >= visibleIndices.Count)
                return -1;
            var index = visibleIndices[list.SelectedIndex];
            return (index >= 0 && index < view.Entries.Length) ? index : -1;
        }
    }

    internal MenuForm(string folder)
    {
        this.folder = folder;
        Text = "Lost Crown Achievement Menu — F8 closes";
        ClientSize = new Size(675, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        KeyPreview = true;
        Controls.AddRange(new Control[] { platformLabel, refresh, list, description, unlock, status });
        refresh.Click += (_, _) => Send(1, -1);
        list.SelectedIndexChanged += (_, _) =>
        {
            var index = SelectedEntryIndex;
            description.Text = view != null && index >= 0 ? view.Entries[index].Description : "";
            unlock.Enabled = view != null && Selection.Valid(new Command("", view.Platform, index, view.ListId), view);
        };
        unlock.Click += (_, _) =>
        {
            var index = SelectedEntryIndex;
            if (view == null || index < 0) return;
            var entry = view.Entries[index];
            if (MessageBox.Show(this, $"Unlock '{entry.Name}' on Ubisoft Connect? This may be permanent.",
                "Unlock achievement", MessageBoxButtons.YesNo) == DialogResult.Yes)
                Send(view.Platform, index);
        };
        KeyDown += (_, e) => { if (e.KeyCode == Keys.F8) Close(); };
        timer.Tick += (_, _) => ReadView();
        FormClosed += (_, _) => timer.Dispose();
        timer.Start();
        ReadView();
        if (view == null || view.Entries.Length == 0) Send(1, -1);
    }

    private void Send(int platform, int index)
    {
        try
        {
            var command = new Command(Guid.NewGuid().ToString(), platform, index, view?.ListId ?? "");
            File.WriteAllText(Path.Combine(folder, "command.tmp"), JsonSerializer.Serialize(command));
            File.Move(Path.Combine(folder, "command.tmp"), Path.Combine(folder, "command.json"), true);
            refresh.Enabled = unlock.Enabled = false;
            status.Text = "Waiting for the game...";
        }
        catch (Exception ex) { status.Text = ex.Message; }
    }

    private void ReadView()
    {
        try
        {
            var json = File.ReadAllText(Path.Combine(folder, "view.json"));
            if (json == lastJson) return;
            var next = JsonSerializer.Deserialize<View>(json);
            if (next == null) return;
            var listChanged = Selection.ListChanged(view, next);
            view = next;
            lastJson = json;
            if (listChanged)
            {
                list.BeginUpdate();
                list.Items.Clear();
                visibleIndices.Clear();
                for (var i = 0; i < view.Entries.Length; i++)
                {
                    var entry = view.Entries[i];
                    if (!entry.Unlocked)
                    {
                        visibleIndices.Add(i);
                        list.Items.Add(entry.Name);
                    }
                }
                list.EndUpdate();
                description.Text = "";
            }
            status.Text = view.Status;
            refresh.Enabled = !view.Busy;
            unlock.Enabled = Selection.Valid(new Command("", view.Platform, SelectedEntryIndex, view.ListId), view);
        }
        catch (IOException) { }
        catch (Exception ex) { status.Text = ex.Message; }
    }
}
