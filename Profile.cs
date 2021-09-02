using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace bagpipe {
  [Flags]
  enum LoadProfileResult {
    OK = 0,
    Assumed = 0b01,
    Unknown = 0b10
  };

  class Profile : ObservableCollection<ProfileEntry> {
    public LoadProfileResult LoadProfile(string path) {
      Clear();

      byte[] decompressedData;

      using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
        long pos = fs.Seek(20, SeekOrigin.Begin);
        if (pos != 20) {
          throw new EndOfStreamException();
        }

        // Don't really know if this is signed or not, but it should never practically matter
        uint size = BinaryPrimitives.ReadUInt32BigEndian(fs.ReadByteArray(4));

        using (MemoryStream ms = new MemoryStream()) {
          fs.CopyTo(ms);
          try {
            decompressedData = LZO.Decompress(ms.ToArray(), (int)size);
          } catch (Win32Exception ex) {
            throw new IOException(ex.Message, ex);
          }
        }
      }

      LoadProfileResult res = LoadProfileResult.OK;

      using (MemoryStream ms = new MemoryStream(decompressedData)) {
        while (ms.Position < decompressedData.Length) {
          ProfileEntry entry = new ProfileEntry();

          // Again don't really know if these are signed or not
          entry.Owner = (OnlineProfilePropertyOwner)ms.ReadByteSafe();
          entry.ID = BinaryPrimitives.ReadUInt32BigEndian(ms.ReadByteArray(4));
          entry.Type = (SettingsDataType)ms.ReadByteSafe();

          switch (entry.Type) {
            case SettingsDataType.Int32: {
              entry.Value = BinaryPrimitives.ReadInt32BigEndian(ms.ReadByteArray(4));
              break;
            }
            case SettingsDataType.String: {
              int len = BinaryPrimitives.ReadInt32BigEndian(ms.ReadByteArray(4));
              entry.Value = Encoding.ASCII.GetString(ms.ReadByteArray(len));
              break;
            }
            case SettingsDataType.Float: {
              entry.Value = BitConverter.Int32BitsToSingle(
                BinaryPrimitives.ReadInt32BigEndian(ms.ReadByteArray(4))
              );
              break;
            }
            case SettingsDataType.Blob: {
              int len = BinaryPrimitives.ReadInt32BigEndian(ms.ReadByteArray(4));
              entry.Value = ms.ReadByteArray(len);
              break;
            }
            case SettingsDataType.Byte: {
              entry.Value = ms.ReadByteSafe();
              break;
            }
            case SettingsDataType.Empty: {
              res |= LoadProfileResult.Assumed;
              break;
            }
            case SettingsDataType.Int64: {
              res |= LoadProfileResult.Assumed;
              entry.Value = BinaryPrimitives.ReadInt64BigEndian(ms.ReadByteArray(8));
              break;
            }
            case SettingsDataType.Double: {
              res |= LoadProfileResult.Assumed;
              entry.Value = BitConverter.Int64BitsToDouble(
                BinaryPrimitives.ReadInt64BigEndian(ms.ReadByteArray(8))
              );
              break;
            }
            default: {
              res |= LoadProfileResult.Unknown;
              break;
            }
          }

          entry.Owner = (OnlineProfilePropertyOwner)ms.ReadByteSafe();

          Add(entry);
        }
      }

      return res;
    }

    public void SaveProfile() {
      // TODO
    }
  }
}
