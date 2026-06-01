using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OneMarkDotNet.AddIn;

public sealed class KeyboardHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _hookProc;
    private bool _disposed;

    public event Action? EnterPressed;
    public event Action? CtrlEnterPressed;
    public event Action? CtrlCommaPressed;
    public event Action? F5Pressed;
    public event Action? F8Pressed;
    public event Action? TabPressed;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_RETURN = 0x0D;
    private const int VK_TAB = 0x09;
    private const int VK_F5 = 0x74;
    private const int VK_F8 = 0x77;
    private const int VK_OEM_COMMA = 0xBC;

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
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_CONTROL = 0x11;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    public KeyboardHook()
    {
        _hookProc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(module.ModuleName), 0);

        if (_hookId == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            AppLogger.Instance.LogError($"Failed to install keyboard hook. Error: {error}");
        }
        else
        {
            AppLogger.Instance.LogInfo("Keyboard hook installed successfully");
        }
    }

    public void Uninstall()
    {
        if (_hookId == IntPtr.Zero) return;

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        AppLogger.Instance.LogInfo("Keyboard hook uninstalled");
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam.ToInt32() == WM_KEYDOWN || wParam.ToInt32() == WM_SYSKEYDOWN))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;

            if (vkCode == VK_RETURN && ctrlPressed)
            {
                CtrlEnterPressed?.Invoke();
                return (IntPtr)1;
            }

            if (vkCode == VK_RETURN)
            {
                EnterPressed?.Invoke();
                return (IntPtr)1;
            }

            if (vkCode == VK_OEM_COMMA && ctrlPressed)
            {
                CtrlCommaPressed?.Invoke();
                return (IntPtr)1;
            }

            if (vkCode == VK_F5)
            {
                F5Pressed?.Invoke();
                return (IntPtr)1;
            }

            if (vkCode == VK_F8)
            {
                F8Pressed?.Invoke();
                return (IntPtr)1;
            }

            if (vkCode == VK_TAB)
            {
                TabPressed?.Invoke();
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Uninstall();
    }
}
