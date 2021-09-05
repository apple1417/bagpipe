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

    public static byte[] Compress(byte[] data, int offset, int len) {
      IntPtr srcPtr = Marshal.AllocHGlobal(len);
      IntPtr dstPtr = Marshal.AllocHGlobal(len + (len / 16) + 64 + 3);

      Marshal.Copy(data, offset, srcPtr, len);

      uint outSize = 0;
      int res = lzo1x_1_compress(srcPtr, (uint)len, dstPtr, ref outSize, workMem);
      if (res != 0) {
        throw new Win32Exception($"Compression failed: {res}");
      }

      byte[] output = new byte[outSize];
      Marshal.Copy(dstPtr, output, 0, (int)outSize);

      Marshal.FreeHGlobal(srcPtr);
      Marshal.FreeHGlobal(dstPtr);

      return output;
    }

    public static byte[] Decompress(int decompressedSize, byte[] data, int offset, int len) {
      IntPtr srcPtr = Marshal.AllocHGlobal(len);
      IntPtr dstPtr = Marshal.AllocHGlobal(decompressedSize);

      Marshal.Copy(data, offset, srcPtr, len);

      uint outSize = (uint)decompressedSize;
      int res = lzo1x_decompress_safe(srcPtr, (uint)len, dstPtr, ref outSize, workMem);
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
