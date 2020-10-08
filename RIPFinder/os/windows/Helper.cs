using System;

namespace RIPFinder.OS.Windows
{
    public class Helper: IHelper
    {
        public IntPtr OpenProcess(int pid)
        {
            return PInvokes.OpenProcess((int)PInvokes.ProcessAccessFlags.PROCESS_VM_READ, false, pid);
        }

        public bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize, ref IntPtr lpNumberOfBytesRead)
        {
            return PInvokes.ReadProcessMemory(hProcess, lpBaseAddress, lpBuffer, nSize, ref lpNumberOfBytesRead);
        }
    }
}