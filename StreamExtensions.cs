using System.IO;

namespace bagpipe {
  static class StreamExtensions {
    public static byte[] ReadByteArray(this Stream stream, int n) {
      byte[] buf = new byte[n];
      int len = stream.Read(buf, 0, n);
      if (len != n) {
        throw new EndOfStreamException();
      }
      return buf;
    }

    public static byte ReadByteSafe(this Stream stream) {
      int val = stream.ReadByte();
      if (val == -1) {
        throw new EndOfStreamException();
      }
      return (byte)val;
    }
  }
}
