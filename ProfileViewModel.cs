using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace bagpipe {
  class ProfileEntryViewModel : ViewModelBase {
    private readonly ProfileEntry entry;
    public ProfileEntryViewModel(ProfileEntry entry, ProfileViewModel profileVM) {
      this.entry = entry;
      UpdateName(profileVM.DisplayGame);

      profileVM.PropertyChanged += (sender, e) => {
        if (e.PropertyName == nameof(ProfileViewModel.DisplayGame)) {
          UpdateName(((ProfileViewModel) sender).DisplayGame);
        }
      };
    }

    private void UpdateName(Game game) {
      Name = (game == Game.None)
        ? $"ID {entry.ID}"
        : KnownSettings.Data.GetValueOrDefault(game)?.GetValueOrDefault(entry.ID).Name ?? $"Unknown ID {entry.ID}";
    }

    private string _name;
    public string Name {
      get => _name;
      private set => SetProperty(ref _name, value);
    }

    public OnlineProfilePropertyOwner Owner {
      get => entry.Owner;
      set => SetProperty(ref entry.Owner, value);
    }

    public SettingsDataType Type {
      get => entry.Type;
      // No setter since I don't want to deal with updating templates live
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

  class ProfileViewModel : ViewModelBase {
    private readonly Profile profile;

    public IDropTarget DropHandler { get; }
    public ObservableCollection<ProfileEntryViewModel> Entries { get; }

    private Game _displayGame;
    public Game DisplayGame {
      get => _displayGame;
      set => SetProperty(ref _displayGame, value);
    }

    public ProfileViewModel(Profile profile) {
      this.profile = profile;

      DropHandler = new ProfileDropHandler(profile, this);
      Entries = new ViewModelObservableCollection<ProfileEntryViewModel, ProfileEntry>(
        profile.Entries,
        e => new ProfileEntryViewModel(e, this)
      );

      profile.ProfileLoaded += (sender, e) => GuessDisplayGame(e.Path);
    }

    public void GuessDisplayGame(string path) {
      FileInfo info = new FileInfo(path);
      if (info.Name == "Player.wsg") {
        DisplayGame = Game.BL1;
        return;
      }

      DirectoryInfo gameFolder = info.Directory;
      while (gameFolder?.Parent != null && gameFolder.Parent.Name != "My Games") {
        gameFolder = gameFolder.Parent;
      }
      switch (gameFolder.Name) {
        case "Borderlands": {
          DisplayGame = Game.BL1;
          return;
        }
        case "Borderlands Game of the Year": {
          DisplayGame = Game.BL1E;
          return;
        }
        case "Borderlands 2": {
          DisplayGame = Game.BL2;
          return;
        }
        case "Borderlands The Pre-Sequel": {
          DisplayGame = Game.TPS;
          return;
        }
        default: {
          break;
        }
      }

      ProfileEntry versionEntry = profile.Entries.FirstOrDefault(e => e.ID == 26);
      if (versionEntry != null && versionEntry.Type == SettingsDataType.Int32) {
        /*
          BL1 1.0 uses 18, 1.5 uses 20
          All the other games use the exact same value on the oldest accessible and current patch
          It's probably pretty unlikely that we find a different value, but use <= just in case
        */
        int version = (int)versionEntry.Value;
        if (version <= 20) {
          DisplayGame = Game.BL1;
          return;
        } else if (version <= 39) {
          DisplayGame = Game.BL1E;
          return;
        } else if (version <= 66) {
          DisplayGame = Game.BL2;
          return;
        } else if (version <= 72) {
          DisplayGame = Game.TPS;
          return;
        }
      }

      if (!profile.Entries.Any(e => e.ID == 129)) { // PlayerFOV (bl2, tps) / FOV (bl1e)
        DisplayGame = Game.BL1;
      } else if (profile.Entries.Any(e => e.ID == 126)) { // ShowCompass
        DisplayGame = Game.BL1E;
      } else if (profile.Entries.Any(e => e.ID == 168)) { // ResetCameraOnSlam
        DisplayGame = Game.TPS;
      } else {
        DisplayGame = Game.BL2;
      }
    }
  }

  class ProfileDropHandler : DefaultDropHandler {
    private readonly Profile profile;
    private readonly ProfileViewModel profileVM;
    public ProfileDropHandler(Profile profile, ProfileViewModel profileVM) {
      this.profile = profile;
      this.profileVM = profileVM;
    }

    public override void Drop(IDropInfo dropInfo) {
      if (dropInfo?.DragInfo == null) {
        return;
      }

      int insertIndex = GetInsertIndex(dropInfo);
      IOrderedEnumerable<ProfileEntryViewModel> selectedItems = (
        ExtractData(dropInfo.Data)
        .Cast<ProfileEntryViewModel>()
        .OrderBy(entryVM => profileVM.Entries.IndexOf(entryVM))
      );

      foreach (ProfileEntryViewModel entryVM in selectedItems) {
        int index = profileVM.Entries.IndexOf(entryVM);
        if (insertIndex > index) {
          insertIndex--;
        }

        profile.Entries.Move(index, insertIndex);
        insertIndex++;
      }
    }
  }
}
