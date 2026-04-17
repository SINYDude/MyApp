using Terminal.Gui;

public class SettingsDialog : Dialog
{
    private readonly SettingsManager _mgr;
    private readonly List<LaunchItem> _items;
    private readonly ListView _list;
    private bool _saved;

    public bool Saved => _saved;

    // Static slots to smuggle button refs past the base() call limitation.
    // Safe because TG is single-threaded.
    private static Button? _pendingSave;
    private static Button? _pendingCancel;

    private static Button[] MakeButtons()
    {
        _pendingSave   = new Button("Save", is_default: true);
        _pendingCancel = new Button("Cancel");
        return [_pendingSave, _pendingCancel];
    }

    public SettingsDialog(SettingsManager mgr, AppSettings settings)
        : base("  Launch Items Settings  ", 0, 0, MakeButtons())
    {
        // Grab refs before anything clears the statics
        var saveBtn   = _pendingSave!;
        var cancelBtn = _pendingCancel!;
        _pendingSave = _pendingCancel = null;

        _mgr   = mgr;
        _items = settings.LaunchItems.Select(CloneItem).ToList();

        Width  = Dim.Percent(80);
        Height = Dim.Percent(80);

        cancelBtn.Clicked += () => Application.RequestStop(this);

        saveBtn.Clicked += () =>
        {
            settings.LaunchItems.Clear();
            settings.LaunchItems.AddRange(_items);
            _mgr.Save(settings);
            _saved = true;
            Application.RequestStop(this);
        };

        var listFrame = new FrameView(" Items — Enter=Edit  A=Add  D=Delete  Shift+Up/Dn=Move ")
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(3),
        };

        _list = new ListView
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = false,
        };

        _list.OpenSelectedItem += (e) => EditItem(e.Item);

        _list.KeyPress += (e) =>
        {
            switch (e.KeyEvent.Key)
            {
                case Key.A:
                case Key.InsertChar:
                    AddItem();
                    e.Handled = true;
                    break;
                case Key.D:
                case Key.DeleteChar:
                    DeleteItem();
                    e.Handled = true;
                    break;
                case Key.S:
                    AddSeparator();
                    e.Handled = true;
                    break;
                case Key.CursorUp | Key.ShiftMask:
                    MoveItem(-1);
                    e.Handled = true;
                    break;
                case Key.CursorDown | Key.ShiftMask:
                    MoveItem(1);
                    e.Handled = true;
                    break;
            }
        };

        listFrame.Add(_list);
        Add(listFrame);
        RefreshList();
    }

    private void RefreshList(int selectIndex = -1)
    {
        var display = _items.Select(i => i.IsSeparator
            ? $"  {new string('\u2500', 28)}"
            : $"  {i.Label,-22}  {i.Command} {i.Args}").ToList();

        _list.SetSource(display);

        if (selectIndex >= 0 && selectIndex < display.Count)
            _list.SelectedItem = selectIndex;
    }

    private void AddItem()
    {
        var item = new LaunchItem { UseShell = true };
        if (RunItemEditor("Add Launch Item", item))
        {
            int idx = Math.Max(0, _list.SelectedItem + 1);
            if (idx > _items.Count) idx = _items.Count;
            _items.Insert(idx, item);
            RefreshList(idx);
        }
    }

    private void EditItem(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        var item = _items[index];
        if (item.IsSeparator) return;
        if (RunItemEditor("Edit Launch Item", item))
            RefreshList(index);
    }

    private void DeleteItem()
    {
        int idx = _list.SelectedItem;
        if (idx < 0 || idx >= _items.Count) return;
        string name = _items[idx].IsSeparator ? "(separator)" : _items[idx].Label;
        if (MessageBox.Query("Delete", $"Remove \"{name}\"?", "Yes", "No") == 0)
        {
            _items.RemoveAt(idx);
            RefreshList(Math.Min(idx, _items.Count - 1));
        }
    }

    private void AddSeparator()
    {
        int idx = Math.Max(0, _list.SelectedItem + 1);
        if (idx > _items.Count) idx = _items.Count;
        _items.Insert(idx, new LaunchItem { IsSeparator = true });
        RefreshList(idx);
    }

    private void MoveItem(int direction)
    {
        int idx    = _list.SelectedItem;
        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= _items.Count) return;
        (_items[idx], _items[newIdx]) = (_items[newIdx], _items[idx]);
        RefreshList(newIdx);
    }

    private static bool RunItemEditor(string title, LaunchItem item)
    {
        bool ok = false;

        // Create buttons BEFORE the dialog — we keep direct refs, no Subviews search needed.
        var okBtn = new Button("OK", is_default: true);
        var cnBtn = new Button("Cancel");
        var dlg   = new Dialog(title, 62, 13, okBtn, cnBtn);

        void AddRow(int y, string lbl, out TextField field)
        {
            dlg.Add(new Label(lbl) { X = 1, Y = y });
            field = new TextField("") { X = 13, Y = y, Width = Dim.Fill(2) };
            dlg.Add(field);
        }

        AddRow(1, "Label:    ", out var tfLabel);
        AddRow(2, "Command:  ", out var tfCmd);
        AddRow(3, "Args:     ", out var tfArgs);
        AddRow(4, "Work Dir: ", out var tfWork);

        var chkShell = new CheckBox("Use Shell Execute", item.UseShell) { X = 1, Y = 5 };
        dlg.Add(chkShell);

        tfLabel.Text = item.Label;
        tfCmd.Text   = item.Command;
        tfArgs.Text  = item.Args;
        tfWork.Text  = item.WorkDir;

        okBtn.Clicked += () =>
        {
            item.Label    = tfLabel.Text?.ToString() ?? "";
            item.Command  = tfCmd.Text?.ToString()   ?? "";
            item.Args     = tfArgs.Text?.ToString()  ?? "";
            item.WorkDir  = tfWork.Text?.ToString()  ?? "";
            item.UseShell = chkShell.Checked;
            ok = true;
            Application.RequestStop(dlg);
        };

        cnBtn.Clicked += () => Application.RequestStop(dlg);

        Application.Run(dlg);
        return ok;
    }

    private static LaunchItem CloneItem(LaunchItem src) => new()
    {
        Label = src.Label, Command = src.Command, Args = src.Args,
        WorkDir = src.WorkDir, UseShell = src.UseShell, IsSeparator = src.IsSeparator,
    };
}
