using System.Runtime.InteropServices;

namespace RDPGW.Extensions;

internal class MarshalExtensions
{
    /// <summary>
    /// Converts a byte array segment into a structure of type T.
    /// </summary>
    /// <typeparam name="T">The type of the structure to convert to.</typeparam>
    /// <param name="data">The byte array segment containing the data.</param>
    /// <returns>The structure of type T, or null if conversion fails.</returns>
    internal static T? StructFromArraySegment<T>(ArraySegment<byte> data)
    {
        // Convert the ArraySegment to a byte array
        byte[] bytes = data.ToArray();

        // Pin the managed memory to prevent garbage collection during the operation
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        T? theStructure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        handle.Free(); // Unpin the memory
        return theStructure;
    }

    /// <summary>
    /// Converts a structure of type T into a byte array segment.
    /// </summary>
    /// <typeparam name="T">The type of the structure to convert.</typeparam>
    /// <param name="str">The structure to convert.</param>
    /// <returns>A byte array segment containing the serialized structure.</returns>
    internal static ArraySegment<byte> StructToArraySegment<T>(T str)
    {
        if (str == null)
            return new ArraySegment<byte>(Array.Empty<byte>()); // Return an empty array segment if input is null

        // Get the size of the structure in bytes
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = IntPtr.Zero;
        try
        {
            // Allocate unmanaged memory and copy the structure into it
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size); // Copy the data to the byte array
        }
        finally
        {
            // Free the unmanaged memory
            Marshal.FreeHGlobal(ptr);
        }
        return new ArraySegment<byte>(arr);
    }
}