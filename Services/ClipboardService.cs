using Jamiras.Components;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Jamiras.Services
{
    [Export(typeof(IClipboardService))]
    internal class ClipboardService : IClipboardService
    {
        const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;

        public void SetData(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (COMException ex)
            {
                if ((uint)ex.ErrorCode != CLIPBRD_E_CANT_OPEN)
                    throw;

                // another application has the clipboard open, wait 100ms and try again.
                Thread.Sleep(100);

                try
                {
                    Clipboard.SetText(text);
                }
                catch (COMException ex2)
                {
                    if ((uint)ex2.ErrorCode != CLIPBRD_E_CANT_OPEN)
                        throw;

                    // another application still has the clipboard open, see if we can force ownership by clearing the clipboard.
                    Clipboard.Clear();

                    // if this doesn't work, let the exception bubble up.
                    Clipboard.SetText(text);
                }
            }
        }

        public string GetText()
        {

            try
            {
                if (Clipboard.ContainsText())
                    return Clipboard.GetText();
            }
            catch (COMException ex)
            {
                if ((uint)ex.ErrorCode != CLIPBRD_E_CANT_OPEN)
                    throw;

                // another application has the clipboard open, wait 100ms and try again.
                Thread.Sleep(100);

                try
                {
                    if (Clipboard.ContainsText())
                        return Clipboard.GetText();
                }
                catch (COMException ex2)
                {
                    if ((uint)ex2.ErrorCode != CLIPBRD_E_CANT_OPEN)
                        throw;

                    // another application still has the clipboard open, act like there's nothing available
                }
            }

            return null;
        }
    }
}
