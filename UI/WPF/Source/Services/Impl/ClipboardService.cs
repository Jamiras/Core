using Jamiras.Components;
using Jamiras.Services;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Jamiras.UI.WPF.Services.Impl
{
    [Export(typeof(IClipboardService))]
    internal class ClipboardService : IClipboardService
    {
        const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;

        public void SetData(string text)
        {
            // https://stackoverflow.com/questions/68666/clipbrd-e-cant-open-error-when-setting-the-clipboard-from-net
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return;
                }
                catch (COMException ex)
                {
                    if ((uint)ex.ErrorCode != CLIPBRD_E_CANT_OPEN)
                        throw;
                }

                Thread.Sleep(10);
            }

            // could not get a handle to the clipboard after 100ms, try a bigger hammer.
            // https://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
            // this has it's own internal loop. if it fails, just report the error.
            try
            {
                Clipboard.SetDataObject(text, true);
            }
            catch (COMException ex)
            {
                if ((uint)ex.ErrorCode != CLIPBRD_E_CANT_OPEN)
                    throw;

                MessageBox.Show(ex.Message, "Copy to Clipboard failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public string GetText()
        {
            // https://stackoverflow.com/questions/68666/clipbrd-e-cant-open-error-when-setting-the-clipboard-from-net
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    if (Clipboard.ContainsText())
                        return Clipboard.GetText();

                    return null;
                }
                catch (COMException ex)
                {
                    if ((uint)ex.ErrorCode != CLIPBRD_E_CANT_OPEN)
                        throw;
                }

                Thread.Sleep(10);
            }

            // another application still has the clipboard open, act like there's nothing available
            return null;
        }
    }
}
