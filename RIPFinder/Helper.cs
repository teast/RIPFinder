using System;
using System.Runtime.InteropServices;
using HelperLinux = RIPFinder.OS.Linux.Helper;
using HelperWindows = RIPFinder.OS.Windows.Helper;

namespace RIPFinder
{
    static class Helper
    {
        private static IHelper _helper;

        static Helper()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _helper = new HelperLinux();
            else
                _helper = new HelperWindows();
        }

        public static IntPtr OpenProcess(int pid)
        {
            return _helper.OpenProcess(pid);
        }

        public static bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize, ref IntPtr lpNumberOfBytesRead)
        {
            return _helper.ReadProcessMemory(hProcess, lpBaseAddress, lpBuffer, nSize, ref lpNumberOfBytesRead);
        }
    }

    internal interface IHelper
    {
        IntPtr OpenProcess(int pid);
        bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize, ref IntPtr lpNumberOfBytesRead);

    }
}
