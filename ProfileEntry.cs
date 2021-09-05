using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace bagpipe {
  class ProfileEntry {
    public OnlineProfilePropertyOwner Owner;
    public int ID;
    public SettingsDataType Type;

    private object _value;
    public object Value {
      get => _value;
      set {
        if (Type switch {
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
        }) {
          _value = value;
        } else {
          throw new ArgumentException($"Tried to set invalid type {value.GetType().Name} for type field {Type}");
        }
      }
    }

    public OnlineDataAdvertisementType AdvertisementType;
  }

  class ProfileEntryViewModel {
    private readonly ProfileEntry entry;
    public ProfileEntryViewModel(ProfileEntry entry) {
      this.entry = entry;
    }

    public string ID {
      get {
        return $"Unknown ID {entry.ID}";
      }
    }

    public object Value {
      get => entry.Value;
      set => entry.Value = value;
    }
  }
}
