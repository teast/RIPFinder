using System.Runtime.InteropServices;

namespace RIPFinder.TextCopy
{
    public static class Clipboard
    {
        public static void SetDataObject(string text)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                LinuxClipboard.SetText(text);
            else
                WindowsClipboard.SetText(text);
        }
    }
}