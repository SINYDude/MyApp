using Terminal.Gui;

public class DashboardView : Toplevel
{
    private readonly SystemMetrics _metrics;
    private readonly SettingsManager _settingsMgr;
    private readonly HeaderPanel _header;
    private readonly QuickLaunchPanel _launch;
    private readonly SysStatsPanel _stats;
    private readonly ProcessPanel _procs;

    public DashboardView()
    {
        _metrics     = new SystemMetrics();
        _settingsMgr = new SettingsManager();

        ApplyTheme();

        _header = new HeaderPanel
        {
            X = 0, Y = 0,
            Width = Dim.Fill(), Height = 3,
        };

        _launch = new QuickLaunchPanel(_settingsMgr)
        {
            X = 0, Y = 3,
            Width = Dim.Percent(27),
            Height = Dim.Fill(1),
        };

        _stats = new SysStatsPanel(_metrics)
        {
            X = Pos.Percent(27), Y = 3,
            Width = Dim.Percent(43),
            Height = Dim.Fill(1),
        };

        _procs = new ProcessPanel(_metrics)
        {
            X = Pos.Percent(70), Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };

        var status = new StatusBar(new StatusItem[]
        {
            new StatusItem(Key.F2,  "~F2~ Settings",  OpenSettings),
            new StatusItem(Key.F5,  "~F5~ Refresh",   ForceRefresh),
            new StatusItem(Key.Esc, "~Esc~ Quit",     () => Application.RequestStop()),
        });

        Add(_header, _launch, _stats, _procs, status);

        Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(1), Tick);
    }

    private void ApplyTheme()
    {
        var drv = Application.Driver;
        var scheme = new ColorScheme
        {
            Normal    = drv.MakeAttribute(Color.Green,       Color.Black),
            Focus     = drv.MakeAttribute(Color.Black,       Color.BrightGreen),
            HotNormal = drv.MakeAttribute(Color.BrightYellow, Color.Black),
            HotFocus  = drv.MakeAttribute(Color.Black,       Color.BrightYellow),
            Disabled  = drv.MakeAttribute(Color.DarkGray,    Color.Black),
        };

        Colors.Base    = scheme;
        Colors.Menu    = scheme;
        Colors.Dialog  = scheme;
        Colors.TopLevel = scheme;
        ColorScheme    = scheme;
    }

    private bool Tick(MainLoop _)
    {
        _header.Update();
        _stats.Update();
        _procs.Update();
        Application.Refresh();
        return true;
    }

    private void ForceRefresh()
    {
        _header.Update();
        _stats.Update();
        _procs.Update();
        _launch.Refresh();
        Application.Refresh();
    }

    private void OpenSettings()
    {
        var settings = _settingsMgr.Load();
        var dlg = new SettingsDialog(_settingsMgr, settings);
        Application.Run(dlg);
        if (dlg.Saved) _launch.Refresh();
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        if (keyEvent.Key == Key.Esc || keyEvent.Key == Key.Q)
        {
            Application.RequestStop();
            return true;
        }
        return base.ProcessKey(keyEvent);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _metrics.Dispose();
        base.Dispose(disposing);
    }
}
