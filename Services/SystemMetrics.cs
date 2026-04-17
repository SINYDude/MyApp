using System.Diagnostics;
using System.Runtime.InteropServices;

public class SystemMetrics : IDisposable
{
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _ramCounter;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();

    private float _cpuPercent;
    private long _availableRamMb;
    private IReadOnlyList<ProcessInfo> _topProcesses = Array.Empty<ProcessInfo>();

    public float CpuPercent       { get { lock (_lock) return _cpuPercent; } }
    public long AvailableRamMb    { get { lock (_lock) return _availableRamMb; } }
    public long TotalRamMb        { get; }
    public float RamPercent       => TotalRamMb > 0 ? (float)(TotalRamMb - AvailableRamMb) / TotalRamMb : 0;
    public TimeSpan Uptime        => TimeSpan.FromMilliseconds(Environment.TickCount64);
    public IReadOnlyList<ProcessInfo> TopProcesses { get { lock (_lock) return _topProcesses; } }

    public SystemMetrics()
    {
        TotalRamMb = GetTotalRamMb();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // prime — first value is always 0
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch { }
        }

        Task.Run(SampleLoop, _cts.Token);
    }

    private async Task SampleLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, _cts.Token);

#pragma warning disable CA1416
                float cpu = _cpuCounter != null ? Math.Clamp(_cpuCounter.NextValue(), 0f, 100f) : 0f;
                long ram = _ramCounter != null ? (long)_ramCounter.NextValue() : 0L;
#pragma warning restore CA1416
                var procs = SampleProcesses();

                lock (_lock)
                {
                    _cpuPercent = cpu;
                    _availableRamMb = ram;
                    _topProcesses = procs;
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private static IReadOnlyList<ProcessInfo> SampleProcesses()
    {
        var list = new List<ProcessInfo>(32);
        foreach (var p in Process.GetProcesses())
        {
            try { list.Add(new ProcessInfo(p.ProcessName, p.WorkingSet64 / 1024 / 1024)); }
            catch { }
            finally { p.Dispose(); }
        }
        list.Sort((a, b) => b.MemoryMb.CompareTo(a.MemoryMb));
        return list.Count > 20 ? list.GetRange(0, 20) : list;
    }

    private static long GetTotalRamMb()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return 0;
        var s = new MEMORYSTATUSEX { dwLength = 64 };
        return GlobalMemoryStatusEx(ref s) ? (long)(s.ullTotalPhys / 1024 / 1024) : 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength, dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile,
                     ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public void Dispose()
    {
        _cts.Cancel();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
    }
}

public record ProcessInfo(string Name, long MemoryMb);
