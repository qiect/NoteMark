using System.Runtime.InteropServices;

namespace OneMarkDotNet.ImportExport;

public static class ClipboardHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalUnlock(IntPtr hMem);

    const uint CF_UNICODETEXT = 13;
    const uint CF_HTML = 49321;
    const uint GMEM_MOVEABLE = 0x0002;

    public static void SetText(string text)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Clipboard operations are only supported on Windows.");

        if (!OpenClipboard(IntPtr.Zero))
            throw new InvalidOperationException("Failed to open clipboard.");

        try
        {
            EmptyClipboard();

            var bytes = System.Text.Encoding.Unicode.GetBytes(text + "\0");
            var hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            if (hMem == IntPtr.Zero)
                throw new InvalidOperationException("Failed to allocate memory for clipboard.");

            var ptr = GlobalLock(hMem);
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to lock memory for clipboard.");

            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
            }
            finally
            {
                GlobalUnlock(hMem);
            }

            if (SetClipboardData(CF_UNICODETEXT, hMem) == IntPtr.Zero)
                throw new InvalidOperationException("Failed to set clipboard data.");
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static Task<string> GetText()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Clipboard operations are only supported on Windows.");

        if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
            return Task.FromResult(string.Empty);

        if (!OpenClipboard(IntPtr.Zero))
            throw new InvalidOperationException("Failed to open clipboard.");

        try
        {
            var handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero)
                return Task.FromResult(string.Empty);

            var ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero)
                return Task.FromResult(string.Empty);

            try
            {
                return Task.FromResult(Marshal.PtrToStringUni(ptr) ?? string.Empty);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static void SetHtml(string html)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Clipboard operations are only supported on Windows.");

        if (!OpenClipboard(IntPtr.Zero))
            throw new InvalidOperationException("Failed to open clipboard.");

        try
        {
            EmptyClipboard();

            var header = $"Version:0.9\nStartHTML:00000000\nEndHTML:00000000\nStartFragment:00000000\nEndFragment:00000000\n";
            var headerLength = System.Text.Encoding.UTF8.GetByteCount(header);
            var startHtml = headerLength.ToString("D8");
            var htmlPrefix = $"<html><body><!--StartFragment-->";
            var htmlSuffix = $"<!--EndFragment--></body></html>";
            var fullHtml = $"Version:0.9\nStartHTML:{startHtml}\nEndHTML:00000000\nStartFragment:00000000\nEndFragment:00000000\n{htmlPrefix}{html}{htmlSuffix}";

            var fullBytes = System.Text.Encoding.UTF8.GetBytes(fullHtml + "\0");
            var startHtmlIdx = System.Text.Encoding.UTF8.GetByteCount($"Version:0.9\nStartHTML:{startHtml}\nEndHTML:00000000\nStartFragment:00000000\nEndFragment:00000000\n");
            var endHtmlIdx = fullBytes.Length - 1;
            var startFragmentIdx = startHtmlIdx + System.Text.Encoding.UTF8.GetByteCount(htmlPrefix);
            var endFragmentIdx = startFragmentIdx + System.Text.Encoding.UTF8.GetByteCount(html);

            var finalHeader = $"Version:0.9\nStartHTML:{startHtmlIdx:D8}\nEndHTML:{endHtmlIdx:D8}\nStartFragment:{startFragmentIdx:D8}\nEndFragment:{endFragmentIdx:D8}\n";
            var finalBytes = System.Text.Encoding.UTF8.GetBytes(finalHeader + htmlPrefix + html + htmlSuffix + "\0");

            var hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)finalBytes.Length);
            if (hMem == IntPtr.Zero)
                throw new InvalidOperationException("Failed to allocate memory for clipboard.");

            var ptr = GlobalLock(hMem);
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to lock memory for clipboard.");

            try
            {
                Marshal.Copy(finalBytes, 0, ptr, finalBytes.Length);
            }
            finally
            {
                GlobalUnlock(hMem);
            }

            SetClipboardData(CF_HTML, hMem);
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static bool ContainsText()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        return IsClipboardFormatAvailable(CF_UNICODETEXT);
    }
}
