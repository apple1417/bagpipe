using System;

namespace bagpipe {
  enum OnlineProfilePropertyOwner {
    None = 0,
    OnlineService = 1,
    Game = 2,
  }

  enum SettingsDataType {
    Empty = 0,
    Int32 = 1,
    Int64 = 2,
    Double = 3,
    String = 4,
    Float = 5,
    Blob = 6,
    DateTime = 7,
    Byte = 8
  }

  enum OnlineDataAdvertisementType {
    DontAdvertise = 0,
    OnlineService = 1,
    QOS = 2,
    OnlineServiceAndQOS = 3,
  }

  static class EnumExtensions {
    public static Type GetAssociatedType(this SettingsDataType dataType) {
      switch (dataType) {
        case SettingsDataType.Int32: return typeof(int);
        case SettingsDataType.Int64: return typeof(long);
        case SettingsDataType.Double: return typeof(double);
        case SettingsDataType.String: return typeof(string);
        case SettingsDataType.Float: return typeof(float);
        case SettingsDataType.Blob: return typeof(byte[]);
        case SettingsDataType.DateTime: return typeof(DateTime);
        case SettingsDataType.Byte: return typeof(byte);
        default: return null;
      }
    }
  }
}
