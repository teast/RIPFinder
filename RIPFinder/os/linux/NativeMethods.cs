using System;
using System.Runtime.InteropServices;

namespace RIPFinder.OS.Linux
{
    internal static class PInvokes
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct iovec
        {
            public IntPtr iov_base;
            public IntPtr iov_len;
        }

        internal const int _SC_IOV_MAX = 60;

        [DllImport("libc.so.6")]
        internal static extern long sysconf(int name);

        [DllImport("libc.so.6")]
        internal static extern IntPtr process_vm_readv(IntPtr pid, iovec[] local_iov, ulong liovcnt, iovec[] remote_iov, ulong riovcnt, ulong flags);

    }
}