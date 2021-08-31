using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace bagpipe {
  static class LZO {
    [DllImport("minilzo.dll", CallingConvention=CallingConvention.Cdecl)]
    private static extern int lzo1x_1_compress(IntPtr src, uint src_len, IntPtr dst, ref uint dst_len, IntPtr wrkmem);

    [DllImport("minilzo.dll", CallingConvention=CallingConvention.Cdecl)]
    private static extern int lzo1x_decompress_safe(IntPtr src, uint src_len, IntPtr dst, ref uint dst_len, IntPtr wrkmem);

    private static IntPtr workMem = Marshal.AllocHGlobal(16384);

    public static byte[] Compress(byte[] data) {
      IntPtr srcPtr = Marshal.AllocHGlobal(data.Length);
      IntPtr dstPtr = Marshal.AllocHGlobal(data.Length + data.Length / 16 + 64 + 3);

      Marshal.Copy(data, 0, srcPtr, data.Length);

      uint outSize = 0;
      int res = lzo1x_1_compress(srcPtr, (uint)data.Length, dstPtr, ref outSize, workMem);
      if (res != 0) {
        throw new Win32Exception($"Compression failed: {res}");
      }

      byte[] output = new byte[outSize];
      Marshal.Copy(dstPtr, output, 0, (int)outSize);

      Marshal.FreeHGlobal(srcPtr);
      Marshal.FreeHGlobal(dstPtr);

      return output;
    }

    public static byte[] Decompress(byte[] data, int decompressedSize) {
      IntPtr srcPtr = Marshal.AllocHGlobal(data.Length);
      IntPtr dstPtr = Marshal.AllocHGlobal(decompressedSize);

      Marshal.Copy(data, 0, srcPtr, data.Length);

      uint outSize = (uint)decompressedSize;
      int res = lzo1x_decompress_safe(srcPtr, (uint)data.Length, dstPtr, ref outSize, workMem);
      if (res != 0) {
        throw new Win32Exception($"Decompression failed: {res}");
      }

      byte[] output = new byte[outSize];
      Marshal.Copy(dstPtr, output, 0, (int)outSize);

      Marshal.FreeHGlobal(srcPtr);
      Marshal.FreeHGlobal(dstPtr);

      return output;
    }
  }
}
