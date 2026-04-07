using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VedionScreenShare.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        // Modifier flags
        public const uint MOD_NONE  = 0x0000;
        public const uint MOD_ALT   = 0x0001;
        public const uint MOD_CTRL  = 0x0002;
        public const uint MOD_SHIFT = 0x0004;

        private HwndSource _source;
        private readonly int _pauseId   = 1;
        private readonly int _snapshotId = 2;

        public event Action OnPauseResumePressed;
        public event Action OnSnapshotPressed;

        public void Register(uint pauseMod, uint pauseKey, uint snapMod, uint snapKey)
        {
            // Create hidden window to receive hotkey messages
            var helper = new HwndSourceParameters("HotkeyWindow")
            {
                Width = 0, Height = 0,
                WindowStyle = 0
            };
            _source = new HwndSource(helper);
            _source.AddHook(WndProc);

            RegisterHotKey(_source.Handle, _pauseId,    pauseMod, pauseKey);
            RegisterHotKey(_source.Handle, _snapshotId, snapMod,  snapKey);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == _pauseId)    { OnPauseResumePressed?.Invoke(); handled = true; }
                if (id == _snapshotId) { OnSnapshotPressed?.Invoke();    handled = true; }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_source != null)
            {
                UnregisterHotKey(_source.Handle, _pauseId);
                UnregisterHotKey(_source.Handle, _snapshotId);
                _source.RemoveHook(WndProc);
                _source.Dispose();
            }
        }
    }
}
