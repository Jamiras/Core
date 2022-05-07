using Jamiras.Components;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Jamiras.Services
{
    [Export(typeof(IBrowserService))]
    internal class BrowserService : IBrowserService
    {
        public void OpenUrl(string url)
        {
            // https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw new NotSupportedException("Cannot determine default browser for this OS.");
            }
        }
    }
}
