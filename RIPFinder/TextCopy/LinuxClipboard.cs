using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RIPFinder.TextCopy
{
    static class LinuxClipboard
    {
        public static async Task SetTextAsync(string text, CancellationToken cancellation)
        {
            var tempFileName = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFileName, text, cancellation);
            try
            {
                if (cancellation.IsCancellationRequested)
                {
                    return;
                }

                BashRunner.Run($"cat {tempFileName} | xclip -i -selection clipboard");
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }

        public static void SetText(string text)
        {
            var tempFileName = Path.GetTempFileName();
            File.WriteAllText(tempFileName, text);
            try
            {
                BashRunner.Run($"cat {tempFileName} | xclip -i -selection clipboard");
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }

        public static string GetText()
        {
            var tempFileName = Path.GetTempFileName();
            try
            {
                BashRunner.Run($"xclip -o -selection clipboard > {tempFileName}");
                return File.ReadAllText(tempFileName);
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }

        public static async Task<string> GetTextAsync(CancellationToken cancellation)
        {
            var tempFileName = Path.GetTempFileName();
            try
            {
                BashRunner.Run($"xclip -o -selection clipboard > {tempFileName}");
                return await File.ReadAllTextAsync(tempFileName, cancellation);
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }
    }
}