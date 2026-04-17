using Terminal.Gui;

public class SysStatsPanel : FrameView
{
    private readonly SystemMetrics _metrics;
    private readonly Label _cpuBar;
    private readonly Label _ramBar;
    private readonly Label _diskBar;
    private readonly Label _uptime;
    private readonly Label _hostInfo;
    private readonly Label _osInfo;

    public SysStatsPanel(SystemMetrics metrics)
    {
        Title = " SYSTEM STATS ";
        _metrics = metrics;

        Add(new Label("CPU:  ") { X = 1, Y = 0 });
        _cpuBar = new Label("") { X = 7, Y = 0 };

        Add(new Label("RAM:  ") { X = 1, Y = 1 });
        _ramBar = new Label("") { X = 7, Y = 1 };

        Add(new Label("DISK: ") { X = 1, Y = 2 });
        _diskBar = new Label("") { X = 7, Y = 2 };

        Add(new Label("") { X = 1, Y = 3 });

        _uptime   = new Label("") { X = 1, Y = 4 };
        _hostInfo = new Label("") { X = 1, Y = 5 };
        _osInfo   = new Label("") { X = 1, Y = 6 };

        Add(_cpuBar, _ramBar, _diskBar, _uptime, _hostInfo, _osInfo);
        Update();
    }

    public void Update()
    {
        _cpuBar.Text = Bar(_metrics.CpuPercent, 24);

        float ramPct = _metrics.RamPercent * 100f;
        long used = _metrics.TotalRamMb - _metrics.AvailableRamMb;
        _ramBar.Text = $"{Bar(ramPct, 24)}  {used:N0}/{_metrics.TotalRamMb:N0}MB";

        _diskBar.Text = Bar(GetDiskPct(), 24);

        var up = _metrics.Uptime;
        _uptime.Text   = $"UPTIME: {up.Days}d {up.Hours:D2}h {up.Minutes:D2}m {up.Seconds:D2}s";
        _hostInfo.Text = $"HOST:   {Environment.MachineName}";
        _osInfo.Text   = $"USER:   {Environment.UserName}";
    }

    private static float GetDiskPct()
    {
        try
        {
            var d = new System.IO.DriveInfo("C");
            if (!d.IsReady) return 0;
            return (float)(d.TotalSize - d.TotalFreeSpace) / d.TotalSize * 100f;
        }
        catch { return 0; }
    }

    internal static string Bar(float pct, int width)
    {
        pct = Math.Clamp(pct, 0f, 100f);
        int filled = (int)(pct / 100f * width);
        var bar = new string('\u2588', filled) + new string('\u2591', width - filled);
        string warn = pct > 85 ? "!" : pct > 60 ? "~" : " ";
        return $"[{bar}] {pct,5:F1}%{warn}";
    }
}
