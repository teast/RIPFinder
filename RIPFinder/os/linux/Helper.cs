using System;
using System.Runtime.InteropServices;

namespace RIPFinder.OS.Linux
{
    public class Helper: IHelper
    {
        private long maxIoVcnt;

        public Helper()
        {
            maxIoVcnt = PInvokes.sysconf(PInvokes._SC_IOV_MAX);
        }

        public IntPtr OpenProcess(int processId)
        {
            return new IntPtr(processId);
        }

        public bool ReadProcessMemory(IntPtr handle, IntPtr address, byte[] buffer, IntPtr size, ref IntPtr nread)
        {
            var iov_size = 2048;
            var piov_size = new IntPtr(iov_size);
            var count = Math.Max((int)Math.Ceiling((decimal)size.ToInt64() / iov_size), 1);

            if (size.ToInt64() < iov_size)
            {
                iov_size = size.ToInt32();
                piov_size = size;
            }

            if (count > maxIoVcnt)
            {
                throw new Exception("Trying to read too much!");
            }

            var bytes = new byte[count,iov_size];
            var local = new PInvokes.iovec[count];
            var pointers = new IntPtr[count];
            var remote = new PInvokes.iovec[1] { new PInvokes.iovec { iov_base = address, iov_len = size } };

            for(var i = 0; i < local.Length; i++)
            {
                pointers[i] = Marshal.AllocHGlobal(iov_size);
                local[i].iov_base = pointers[i];
                local[i].iov_len = piov_size;
            }

            local[local.Length- 1].iov_len = new IntPtr(iov_size - ((iov_size * count) - size.ToInt64()));

            nread = PInvokes.process_vm_readv(handle, local, (ulong)local.Length, remote, (ulong)remote.Length, 0);

            for(var i = 0; i < local.Length; i++)
            {
                Marshal.Copy(pointers[i], buffer, (i*iov_size), local[i].iov_len.ToInt32());
                Marshal.FreeHGlobal(pointers[i]);
            }

            return nread.ToInt64() == remote[0].iov_len.ToInt64();
        }
    }
}