using System.Runtime.InteropServices;

namespace RDPGW.Extensions;

internal class MarshalExtensions
{
    internal static T? StructFromArraySegment<T>(ArraySegment<byte> data)
    {
        byte[] bytes = data.ToArray();
        // Pin the managed memory while, copy it out the data, then unpin it
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        T? theStructure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        handle.Free();
        return theStructure;
    }

    internal static ArraySegment<byte> StructToArraySegment<T>(T str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return new ArraySegment<byte>(arr);
    }
}