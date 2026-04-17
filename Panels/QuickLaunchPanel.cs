using Terminal.Gui;
using System.Diagnostics;

public class QuickLaunchPanel : FrameView
{
    private readonly SettingsManager _mgr;
    private AppSettings _settings;
    private readonly ListView _list;

    public QuickLaunchPanel(SettingsManager mgr)
    {
        Title = " LAUNCH ";
        _mgr      = mgr;
        _settings = mgr.Load();

        _list = new ListView
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = true,   // Space toggles a mark
        };

        _list.OpenSelectedItem += OnOpen;
        _list.KeyPress         += OnKey;
        Add(_list);
        Refresh();
    }

    public void Refresh()
    {
        var display = _settings.LaunchItems
            .Select(i => i.IsSeparator ? $" {new string('\u2500', 22)}" : $" {i.Label}")
            .ToList();

        int sel = _list.SelectedItem;
        _list.SetSource(display);
        if (sel < display.Count) _list.SelectedItem = sel;
    }

    // Enter — launch all marked items, or just the current one if none marked
    private void OnOpen(ListViewItemEventArgs e)
    {
        var marked = GetMarkedIndexes();

        if (marked.Count == 0)
        {
            LaunchOne(e.Item);
            return;
        }

        // Fire-and-forget: each Process.Start returns immediately
        foreach (int idx in marked)
            LaunchOne(idx);

        ClearAllMarks();
    }

    private void OnKey(KeyEventEventArgs e)
    {
        if (e.KeyEvent.Key == Key.F2)
        {
            OpenSettings();
            e.Handled = true;
        }
    }

    private List<int> GetMarkedIndexes()
    {
        var result = new List<int>();
        if (_list.Source == null) return result;
        for (int i = 0; i < _settings.LaunchItems.Count; i++)
            if (_list.Source.IsMarked(i)) result.Add(i);
        return result;
    }

    private void ClearAllMarks()
    {
        if (_list.Source == null) return;
        for (int i = 0; i < _settings.LaunchItems.Count; i++)
            _list.Source.SetMark(i, false);
        SetNeedsDisplay();
    }

    private void LaunchOne(int index)
    {
        if (index < 0 || index >= _settings.LaunchItems.Count) return;
        var item = _settings.LaunchItems[index];
        if (item.IsSeparator || string.IsNullOrWhiteSpace(item.Command)) return;

        try
        {
            // Process.Start for GUI apps is fire-and-forget — returns immediately
            // and the launched program runs independently in its own window.
            Process.Start(new ProcessStartInfo
            {
                FileName         = item.Command,
                Arguments        = item.Args,
                UseShellExecute  = item.UseShell,
                WorkingDirectory = string.IsNullOrWhiteSpace(item.WorkDir)
                                   ? null : item.WorkDir,
            });
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Launch Failed", $"{item.Label}:\n{ex.Message}", "OK");
        }
    }

    private void OpenSettings()
    {
        var dlg = new SettingsDialog(_mgr, _settings);
        Application.Run(dlg);
        if (dlg.Saved)
        {
            _settings = _mgr.Load();
            Refresh();
        }
    }
}
