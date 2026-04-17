using System.Text.Json;
using System.Text.Json.Serialization;

public class SettingsManager
{
    private static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return CreateDefaults();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? CreateDefaults();
        }
        catch
        {
            return CreateDefaults();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        File.WriteAllText(SettingsPath, json);
    }

    private static AppSettings CreateDefaults() => new()
    {
        LaunchItems =
        [
            new LaunchItem { Label = "Windows Terminal", Command = "wt.exe" },
            new LaunchItem { Label = "PowerShell",       Command = "pwsh.exe" },
            new LaunchItem { Label = "File Explorer",    Command = "explorer.exe" },
            new LaunchItem { Label = "Edge Browser",     Command = "msedge.exe" },
            new LaunchItem { Label = "Thorium Browser",  Command = "%LOCALAPPDATA%\\Thorium\\Application\\thorium.exe", UseShell = true },
            new LaunchItem { Label = "VS Code",          Command = "code" },
            new LaunchItem { Label = "Notepad",          Command = "notepad.exe" },
            new LaunchItem { Label = "Task Manager",     Command = "taskmgr.exe" },
            new LaunchItem { Label = "Calculator",       Command = "calc.exe" },
            new LaunchItem { Label = "Settings",         Command = "ms-settings:", UseShell = true },
            new LaunchItem { IsSeparator = true },
            new LaunchItem { Label = "Lock Screen",      Command = "rundll32.exe", Args = "user32.dll,LockWorkStation" },
            new LaunchItem { Label = "Restart",          Command = "shutdown.exe", Args = "/r /t 0" },
            new LaunchItem { Label = "Shutdown",         Command = "shutdown.exe", Args = "/s /t 0" },
        ]
    };
}

public class AppSettings
{
    public List<LaunchItem> LaunchItems { get; set; } = [];
}

public class LaunchItem
{
    public string Label     { get; set; } = "";
    public string Command   { get; set; } = "";
    public string Args      { get; set; } = "";
    public string WorkDir   { get; set; } = "";
    public bool   UseShell  { get; set; } = true;
    public bool   IsSeparator { get; set; }
}
