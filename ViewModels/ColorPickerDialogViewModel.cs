using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Jamiras.ViewModels
{
    /// <summary>
    /// ViewModel for displaying the Windows Color Picker dialog.
    /// </summary>
    public sealed class ColorPickerDialogViewModel : DialogViewModelBase
    {
        public System.Windows.Media.Color SelectedColor { get; set; }

        [StructLayout(LayoutKind.Sequential)]
        private struct COLORREF
        {
            public byte R;
            public byte G;
            public byte B;
        }

        private delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class CHOOSECOLOR 
        { 
            public int lStructSize = Marshal.SizeOf(typeof(CHOOSECOLOR));
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public COLORREF rgbResult;
            public IntPtr lpCustColors;
            public uint Flags;
            public IntPtr lCustData;
            public WndProc lpfnHook;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpTemplateName; 
        }

        private static int[] _customColors = new int[16];

        private static readonly uint CC_RGBINIT = 0x000001;
        private static readonly uint CC_FULLOPEN = 0x000002;

        [DllImport("comdlg32.dll", EntryPoint = "ChooseColorW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChooseColor([In, Out] CHOOSECOLOR lpchoosecolor);

        public new DialogResult ShowDialog()
        {
            if (_dialogService.HasDialogHandler(typeof(ColorPickerDialogViewModel)))
                return _dialogService.ShowDialog(this);

            var topMostWindow = _dialogService.GetTopMostDialog() ?? _dialogService.MainWindow;
            var windowInteropHelper = new WindowInteropHelper(topMostWindow);

            var customColors = Marshal.AllocCoTaskMem(_customColors.Length * Marshal.SizeOf(_customColors[0]));
            Marshal.Copy(_customColors, 0, customColors, _customColors.Length);

            var chooseColor = new CHOOSECOLOR();
            chooseColor.hwndOwner = windowInteropHelper.Handle;
            chooseColor.rgbResult.R = SelectedColor.R;
            chooseColor.rgbResult.G = SelectedColor.G;
            chooseColor.rgbResult.B = SelectedColor.B;
            chooseColor.lpCustColors = customColors;
            chooseColor.Flags = CC_FULLOPEN | CC_RGBINIT;

            var result = DialogResult.Cancel;
            if (ChooseColor(chooseColor))
            {
                SelectedColor = System.Windows.Media.Color.FromRgb(chooseColor.rgbResult.R, chooseColor.rgbResult.G, chooseColor.rgbResult.B);
                result = DialogResult.Ok;
            }

            Marshal.Copy(customColors, _customColors, 0, _customColors.Length);
            Marshal.FreeCoTaskMem(customColors);

            return result;
        }
    }
}
