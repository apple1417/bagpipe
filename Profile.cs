using System;
using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace bagpipe {
  class ProfileUpdateEventArgs : EventArgs {
    public string Path;
    public ProfileUpdateEventArgs(string Path) {
      this.Path = Path;
    }
  }

  class Profile {
    public event EventHandler<ProfileUpdateEventArgs> ProfileLoaded;
    public event EventHandler<ProfileUpdateEventArgs> ProfileSaved;

    public readonly ObservableCollection<ProfileEntry> Entries = new ObservableCollection<ProfileEntry>();

    public bool Load(string path) {
      Entries.Clear();

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

      bool unknownData = false;

      using (MemoryStream ms = new MemoryStream(decompressedData)) {
        int entryCount = BinaryPrimitives.ReadInt32BigEndian(ms.ReadByteArray(4));

        for (int i = 0; i < entryCount; i++) {
          ProfileEntry entry = new ProfileEntry();

          entry.Owner = (OnlineProfilePropertyOwner)ms.ReadByteSafe();
          if (
            entry.Owner != OnlineProfilePropertyOwner.Game
            && entry.Owner != OnlineProfilePropertyOwner.OnlineService
          ) {
            unknownData = true;
          }

          entry.ID = BinaryPrimitives.ReadInt32BigEndian(ms.ReadByteArray(4));
          entry.Type = (SettingsDataType)ms.ReadByteSafe();

          switch (entry.Type) {
            // This is signed (confirmed via testing badass rank)
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
            // Haven't encountered these in practice, but can make decent educated guesses
            case SettingsDataType.Empty: {
              unknownData = true;
              break;
            }
            case SettingsDataType.Int64: {
              unknownData = true;
              entry.Value = BinaryPrimitives.ReadInt64BigEndian(ms.ReadByteArray(8));
              break;
            }
            case SettingsDataType.Double: {
              unknownData = true;
              entry.Value = BitConverter.Int64BitsToDouble(
                BinaryPrimitives.ReadInt64BigEndian(ms.ReadByteArray(8))
              );
              break;
            }
            /*
            This one's harder to guess, the only definite hint we have is getters/setters which take two seperate int values
            UnrealScript ints are 32bit, so we're probably looking at 8 bytes data

            UE4 has a FDateTime struct, which uses int64 ticks - maybe it just splits this value in two out of neccesity?
            https://docs.unrealengine.com/4.26/en-US/API/Runtime/Core/Misc/FDateTime/
            Very conveniently, this seems to use the exact same format as dotnet's DateTime

            In practice, nothing ever sets values of this type, so doesn't really matter if this is wrong
            */
            case SettingsDataType.DateTime: {
              unknownData = true;
              entry.Value = new DateTime(BinaryPrimitives.ReadInt64BigEndian(ms.ReadByteArray(8)));
              break;
            }
            default: {
              unknownData = true;
              break;
            }
          }

          entry.AdvertisementType = (OnlineDataAdvertisementType)ms.ReadByteSafe();
          if (entry.AdvertisementType != OnlineDataAdvertisementType.DontAdvertise) {
            unknownData = true;
          }

          Entries.Add(entry);
        }

        if (ms.Position != ms.Length) {
          bool allowed = false;

          // Allow a single trailing 1 (AoDK magic)
          if (ms.Length - ms.Position == 1) {
            int val = ms.ReadByte();
            if (val == 1) {
              allowed = true;
              // Don't reset position so the zero padding check doesn't include it
            } else {
              ms.Position--;
            }
          }

          // Allow trailing zero padding
          allowed |= ms.ReadByteArray((int)(ms.Length - ms.Position)).All(x => x == 0);

          if (!allowed) {
            unknownData = true;
          }
        }
      }

      ProfileLoaded?.Invoke(this, new ProfileUpdateEventArgs(path));

      return unknownData;
    }

    private byte[] GetDecompressedData() {
      using (MemoryStream ms = new MemoryStream()) {
        void WriteInt32(int val) {
          byte[] buf = new byte[4];
          BinaryPrimitives.WriteInt32BigEndian(buf, val);
          ms.Write(buf);
        }
        void WriteInt64(long val) {
          byte[] buf = new byte[8];
          BinaryPrimitives.WriteInt64BigEndian(buf, val);
          ms.Write(buf);
        }

        WriteInt32(Entries.Count);

        foreach (ProfileEntry entry in Entries) {
          ms.WriteByte((byte)entry.Owner);
          WriteInt32(entry.ID);
          ms.WriteByte((byte)entry.Type);

          switch (entry.Type) {
            case SettingsDataType.Int32: {
              WriteInt32((int)entry.Value);
              break;
            }
            case SettingsDataType.String: {
              WriteInt32(((string)entry.Value).Length);
              ms.Write(Encoding.ASCII.GetBytes((string)entry.Value));
              break;
            }
            case SettingsDataType.Float: {
              WriteInt32(BitConverter.SingleToInt32Bits((float)entry.Value));
              break;
            }
            case SettingsDataType.Blob: {
              WriteInt32(((byte[])entry.Value).Length);
              ms.Write((byte[])entry.Value);
              break;
            }
            case SettingsDataType.Byte: {
              ms.WriteByte((byte)entry.Value);
              break;
            }
            // Educated guesses
            case SettingsDataType.Empty: {
              break;
            }
            case SettingsDataType.Int64: {
              WriteInt64((long)entry.Value);
              break;
            }
            case SettingsDataType.Double: {
              WriteInt64(BitConverter.DoubleToInt64Bits((double)entry.Value));
              break;
            }
            case SettingsDataType.DateTime: {
              WriteInt64(((DateTime)entry.Value).Ticks);
              break;
            }
            default: {
              throw new IOException($"Unable to encode entry of type {entry.Type}!");
            }
          }

          ms.WriteByte((byte)entry.AdvertisementType);
        }

        ms.WriteByte(1);

        return ms.ToArray();
      }
    }

    public bool IsOverSizeLimit() => GetDecompressedData().Length > 9000;

    public void Save(string path) {
      byte[] decompressed = GetDecompressedData();
      byte[] compressed;

      try {
        compressed = LZO.Compress(decompressed, 0, decompressed.Length);
      } catch (Win32Exception ex) {
        throw new IOException(ex.Message, ex);
      }

      using (SHA1Managed sha1 = new SHA1Managed()) {
        byte[] decompressedSize = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(decompressedSize, decompressed.Length);

        sha1.TransformBlock(decompressedSize, 0, decompressedSize.Length, null, 0);
        sha1.TransformFinalBlock(compressed, 0, compressed.Length);

        using (FileStream fs = new FileStream(path, FileMode.Create)) {
          fs.Write(sha1.Hash);
          fs.Write(decompressedSize);
          fs.Write(compressed);
        }

        ProfileSaved?.Invoke(this, new ProfileUpdateEventArgs(path));
      }
    }
  }
}
