using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace bagpipe {
  class ProfileEntry {
    public OnlineProfilePropertyOwner Owner;
    public uint ID;
    // TODO: Validate Type
    public SettingsDataType Type;
    public object Value;
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
      get {
        return entry.Value;
      }
      set {
        // TODO: validate type
        entry.Value = value;
      }
    }
  }
}
