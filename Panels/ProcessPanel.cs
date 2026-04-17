using Terminal.Gui;

public class ProcessPanel : FrameView
{
    private readonly SystemMetrics _metrics;
    private readonly ListView _list;

    public ProcessPanel(SystemMetrics metrics)
    {
        Title = " TOP PROCESSES (MEM) ";
        _metrics = metrics;

        var header = new Label($"{"Name",-20} {"MB",8}") { X = 0, Y = 0 };
        var sep    = new Label(new string('\u2500', 30))  { X = 0, Y = 1 };

        _list = new ListView
        {
            X = 0, Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        Add(header, sep, _list);
        Update();
    }

    public void Update()
    {
        var procs = _metrics.TopProcesses;
        var items = new System.Collections.Generic.List<string>(procs.Count);
        foreach (var p in procs)
            items.Add($"{p.Name,-20} {p.MemoryMb,8:N0}");

        if (items.Count == 0) items.Add("Sampling...");

        int sel = _list.SelectedItem;
        _list.SetSource(items);
        if (sel < items.Count) _list.SelectedItem = sel;
    }
}
