namespace OneMarkDotNet
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_F5 = 0x74;
        private const int VK_F8 = 0x77;
        private const int VK_M = 0x4D;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        private IntPtr hookId = IntPtr.Zero;
        private LowLevelKeyboardProc hookProc;
        private bool disposed;
        private int lastOneNoteCheckTickCount;
        private bool cachedIsOneNoteActive;

        public event EventHandler RenderRequested;
        public event EventHandler ExportRequested;
        public event EventHandler SourceModeRequested;

        public KeyboardHook()
        {
            hookProc = HookCallbackProcedure;
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                hookId = SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, GetModuleHandle(module.ModuleName), 0);
            }
        }

        private bool IsOneNoteActive()
        {
            // Cache the check for 500ms to avoid excessive Win32 calls
            var tick = Environment.TickCount;
            if (Math.Abs(tick - lastOneNoteCheckTickCount) < 500)
            {
                return cachedIsOneNoteActive;
            }

            lastOneNoteCheckTickCount = tick;

            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    cachedIsOneNoteActive = false;
                    return false;
                }

                var className = new System.Text.StringBuilder(256);
                GetClassName(hwnd, className, 256);
                var name = className.ToString();

                cachedIsOneNoteActive = name.StartsWith("OneNote", StringComparison.OrdinalIgnoreCase) ||
                                        name.IndexOf("ONENOTE", StringComparison.OrdinalIgnoreCase) >= 0;
                return cachedIsOneNoteActive;
            }
            catch
            {
                cachedIsOneNoteActive = false;
                return false;
            }
        }

        private IntPtr HookCallbackProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                var kbStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                var vkCode = kbStruct.vkCode;

                // Only check OneNote active for function keys and Ctrl combos
                if (vkCode == VK_F5 || vkCode == VK_F8 || (vkCode == VK_M && (Control.ModifierKeys & Keys.Control) == Keys.Control))
                {
                    if (IsOneNoteActive())
                    {
                        var ctrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                        var shiftPressed = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
                        var altPressed = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

                        // F5: Render selection
                        if (vkCode == VK_F5 && !ctrlPressed && !shiftPressed && !altPressed)
                        {
                            OnRenderRequested();
                            return (IntPtr)1;
                        }

                        // F8: Export to clipboard
                        if (vkCode == VK_F8 && !ctrlPressed && !shiftPressed && !altPressed)
                        {
                            OnExportRequested();
                            return (IntPtr)1;
                        }

                        // Ctrl+Shift+M: Toggle source mode
                        if (vkCode == VK_M && ctrlPressed && shiftPressed && !altPressed)
                        {
                            OnSourceModeRequested();
                            return (IntPtr)1;
                        }
                    }
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private void OnRenderRequested()
        {
            var handler = RenderRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnExportRequested()
        {
            var handler = ExportRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnSourceModeRequested()
        {
            var handler = SourceModeRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(hookId);
                    hookId = IntPtr.Zero;
                }

                disposed = true;
            }
        }

        ~KeyboardHook()
        {
            Dispose(false);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
    }
}
