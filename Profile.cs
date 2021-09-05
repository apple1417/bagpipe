using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace bagpipe {
  class Profile : ObservableCollection<ProfileEntry> {
    public bool LoadProfile(string path) {
      Clear();

      byte[] decompressedData;

      using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
        fs.SeekSafe(20, SeekOrigin.Begin);

        // Don't really know if this is signed or not, but it should never practically matter
        int size = BinaryPrimitives.ReadInt32BigEndian(fs.ReadByteArray(4));

        using (MemoryStream ms = new MemoryStream()) {
          fs.CopyTo(ms);
          try {
            decompressedData = LZO.Decompress(size, ms.GetBuffer(), 0, (int)ms.Length);
          } catch (Win32Exception ex) {
            throw new IOException(ex.Message, ex);
          }
        }
      }

      bool warning = false;

      using (MemoryStream ms = new MemoryStream(decompressedData)) {
        int entryCount = BinaryPrimitives.ReadInt32BigEndian(ms.ReadByteArray(4));

        for (int i = 0; i < entryCount; i++) {
          ProfileEntry entry = new ProfileEntry();

          entry.Owner = (OnlineProfilePropertyOwner)ms.ReadByteSafe();
          if (
            entry.Owner != OnlineProfilePropertyOwner.Game
            || entry.Owner != OnlineProfilePropertyOwner.OnlineService
          ) {
            warning = true;
          }

          entry.ID = BinaryPrimitives.ReadInt32BigEndian(ms.ReadByteArray(4));
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
              warning = true;
              break;
            }
            case SettingsDataType.Int64: {
              warning = true;
              entry.Value = BinaryPrimitives.ReadInt64BigEndian(ms.ReadByteArray(8));
              break;
            }
            case SettingsDataType.Double: {
              warning = true;
              entry.Value = BitConverter.Int64BitsToDouble(
                BinaryPrimitives.ReadInt64BigEndian(ms.ReadByteArray(8))
              );
              break;
            }
            default: {
              warning = true;
              break;
            }
          }

          entry.AdvertisementType = (OnlineDataAdvertisementType)ms.ReadByteSafe();
          if (entry.AdvertisementType != OnlineDataAdvertisementType.DontAdvertise) {
            warning = true;
          }

          Add(entry);
        }

        if (ms.Position != ms.Length) {
          warning = true;
        }
      }

      return warning;
    }

    public void SaveProfile() {
      // TODO
    }
  }
}
