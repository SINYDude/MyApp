using Terminal.Gui;

public class HeaderPanel : FrameView
{
    private readonly Label _date;
    private readonly Label _time;

    public HeaderPanel()
    {
        Title = " COMMAND CENTER ";
        CanFocus = false;

        var title = new Label(" >> SYSTEM DASHBOARD <<") { X = 1, Y = 0 };
        _date = new Label("") { X = Pos.Center(), Y = 0 };
        _time = new Label("") { X = Pos.AnchorEnd(9), Y = 0 };

        Add(title, _date, _time);
        Update();
    }

    public void Update()
    {
        var now = DateTime.Now;
        _date.Text = now.ToString("ddd, MMM dd yyyy");
        _time.Text = now.ToString("HH:mm:ss");
    }
}
