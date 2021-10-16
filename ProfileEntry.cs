using System;

namespace bagpipe {
  class ProfileEntry {
    public OnlineProfilePropertyOwner Owner;
    public int ID;
    public SettingsDataType Type;

    private object _value;
    public object Value {
      get => _value;
      set {
        if (IsValidValue(value)) {
          _value = value;
        } else {
          string typeName = value?.GetType()?.Name ?? "null";
          throw new ArgumentException($"Tried to set invalid type {typeName} for type field {Type}");
        }
      }
    }

    public OnlineDataAdvertisementType AdvertisementType;

    public bool IsValidValue(object value) => Type switch {
      SettingsDataType.Empty => value is null,
      SettingsDataType.Int32 => value is int,
      SettingsDataType.Int64 => value is long,
      SettingsDataType.Double => value is double,
      SettingsDataType.String => value is string,
      SettingsDataType.Float => value is float,
      SettingsDataType.Blob => value is byte[],
      SettingsDataType.DateTime => value is DateTime,
      SettingsDataType.Byte => value is byte,
      _ => false,
    };
  }
}
