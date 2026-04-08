using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VedionScreenShare.Services;

public class HotkeyService : IDisposable
{
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;

    private HwndSource? _source;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 9000;
    private IntPtr _hwnd;

    public void Attach(Window window)
    {
        _hwnd   = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);
    }

    public int Register(uint mod, uint vk, Action callback)
    {
        int id = _nextId++;
        if (!RegisterHotKey(_hwnd, id, mod, vk))
            throw new InvalidOperationException($"Failed to register hotkey (mod={mod:X}, vk={vk:X}). It may already be in use.");
        _handlers[id] = callback;
        return id;
    }

    public void Unregister(int id)
    {
        if (_handlers.Remove(id))
            UnregisterHotKey(_hwnd, id);
    }

    public void UnregisterAll()
    {
        foreach (var id in _handlers.Keys.ToList())
            UnregisterHotKey(_hwnd, id);
        _handlers.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var cb))
        {
            cb();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source?.RemoveHook(WndProc);
    }
}
