using System.Collections.Generic;

namespace bagpipe {
  class ProfileEntryViewModel : ViewModelBase {
    private readonly ProfileEntry entry;
    public ProfileEntryViewModel(ProfileEntry entry, ProfileViewModel profileVM) {
      this.entry = entry;
      UpdateName(profileVM.DisplayGame);

      profileVM.PropertyChanged += (sender, e) => {
        if (e.PropertyName == nameof(ProfileViewModel.DisplayGame)) {
          UpdateName(((ProfileViewModel)sender).DisplayGame);
        }
      };
    }

    private void UpdateName(Game game) {
      Name = (game == Game.None)
        ? $"ID {entry.ID}"
        : KnownSettings.ByGame.GetValueOrDefault(game)?.GetValueOrDefault(entry.ID)?.Name ?? $"Unknown ID {entry.ID}";
    }

    // These don't get setters since updating them live gets awkward
    public int ID => entry.ID;
    // Would also need to update data template for this
    public SettingsDataType Type => entry.Type;

    private string _name;
    public string Name {
      get => _name;
      private set => SetProperty(ref _name, value);
    }

    public OnlineProfilePropertyOwner Owner {
      get => entry.Owner;
      set => SetProperty(ref entry.Owner, value);
    }

    public object Value {
      get => entry.Value;
      set {
        if (entry.Value != value) {
          bool dateTimeNull = Type == SettingsDataType.DateTime && value is null;
          ValidationCheck(!dateTimeNull, "Unable to convert to valid time!");
          ValidationCheck(dateTimeNull || entry.IsValidValue(value), $"Invalid value for data type {Type}!");

          if (PropertyValid()) {
            entry.Value = value;
            InvokePropertyChanged();
          }
        }
      }
    }

    public OnlineDataAdvertisementType AdvertisementType {
      get => entry.AdvertisementType;
      set => SetProperty(ref entry.AdvertisementType, value);
    }
  }
}
