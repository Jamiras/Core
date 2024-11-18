using Jamiras.Components;
using Jamiras.Services;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Jamiras.UI.WPF.Services.Impl
{
    [Export(typeof(IClipboardService))]
    internal class ClipboardService : IClipboardService
    {
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern int GlobalSize(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;

        public void SetData(string text)
        {
            // https://stackoverflow.com/questions/68666/clipbrd-e-cant-open-error-when-setting-the-clipboard-from-net
            // The .NET Clipboard functions demand UIPermissionClipboard.AllClipboard permission, but we only need
            // UIPermissionClipboard.OwnClipboard. This can lead to CLIPBRD_E_CANT_OPEN errors if another application
            // is actively monitoring the clipboard. Rather than try to fight with the other application, call the
            // Win32 APIs directly without demanding the elevated permissions.

            // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setclipboarddata
            // https://www.pinvoke.net/default.aspx/user32/SetClipboardData.html
            // https://stackoverflow.com/a/24698804
            // We have to get the data into an HGLOBAL, which the system will own after we call
            // SetClipboardData so we don't need to free it.
            var strBytes = Encoding.Unicode.GetBytes(text + "\0");
            var hGlobal = Marshal.AllocHGlobal(strBytes.Length);
            if (hGlobal != IntPtr.Zero)
            {
                Marshal.Copy(strBytes, 0, hGlobal, strBytes.Length);

                if (OpenClipboard(IntPtr.Zero))
                {
                    SetClipboardData(CF_UNICODETEXT, hGlobal);
                    CloseClipboard();
                }
                else
                {
                    Marshal.FreeHGlobal(hGlobal);
                }
            }
        }

        public string GetText()
        {
            string result = null;

            // https://www.pinvoke.net/default.aspx/user32.getclipboarddata
            // https://stackoverflow.com/a/33007795
            // https://stackoverflow.com/a/47346795
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
                return null;

            if (OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    IntPtr hClipboardData = GetClipboardData(CF_UNICODETEXT);
                    if (hClipboardData != IntPtr.Zero)
                    {
                        IntPtr hPointer = GlobalLock(hClipboardData);
                        if (hPointer != IntPtr.Zero)
                        {
                            try
                            {
                                result = Marshal.PtrToStringUni(hPointer);
                            }
                            finally
                            {
                                GlobalUnlock(hClipboardData);
                            }
                        }
                    }
                }
                finally
                {
                    CloseClipboard();
                }
            }

            return result;
        }
    }
}
