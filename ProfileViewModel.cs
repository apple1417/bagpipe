using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

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
        : KnownSettings.Data.GetValueOrDefault(game)?.GetValueOrDefault(entry.ID)?.Name ?? $"Unknown ID {entry.ID}";
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

  class ProfileViewModel : ViewModelBase {
    private readonly Profile profile;

    public IDropTarget DropHandler { get; }
    public ObservableCollection<ProfileEntryViewModel> Entries { get; }

    public ProfileViewModel(Profile profile) {
      this.profile = profile;

      DropHandler = new ProfileDropHandler(profile, this);
      Entries = new ViewModelObservableCollection<ProfileEntryViewModel, ProfileEntry>(
        profile.Entries,
        e => new ProfileEntryViewModel(e, this)
      );

      Entries.CollectionChanged += (sender, e) => {
        UpdateWatchedEntries();

        InvokePropertyChanged(nameof(HasCustomizations));
      };

      profile.ProfileLoaded += (sender, e) => {
        GuessDisplayGame(e.Path);
      };
    }

    #region Game
    private Game _displayGame;
    public Game DisplayGame {
      get => _displayGame;
      set => SetProperty(ref _displayGame, value);
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
    #endregion

    #region Customizations
    public bool HasCustomizations => Entries.Any(x => KnownSettings.UnlockedCustomizations.Matches(x.ID, x.Type));

    // TODO: Validate
    private static readonly byte[] NO_CUSTOMIZATIONS = Enumerable.Repeat((byte)0, 1001).ToArray();
    private static readonly byte[] ALL_CUSTOMIZATIONS = Enumerable.Repeat((byte)0xFF, 1001).ToArray();

    public void UpdateCustomizations(bool unlock) {
      ProfileEntryViewModel entry = Entries.FirstOrDefault(x => KnownSettings.UnlockedCustomizations.Matches(x.ID, x.Type));
      if (entry != null) {
        entry.Value = unlock ? ALL_CUSTOMIZATIONS : NO_CUSTOMIZATIONS;
      }
    }
    #endregion

    #region Watched Entry Updates
    private Dictionary<KnownSettingInfo, PropertyChangedEventHandler> watchedEntryCallbacks = new Dictionary<KnownSettingInfo, PropertyChangedEventHandler>();

    private void UpdateWatchedEntries() {
      void UpdateWatchedEntry(KnownSettingInfo data, string property, ref ProfileEntryViewModel entry) {
        ProfileEntryViewModel match = Entries.FirstOrDefault(x => data.Matches(x.ID, x.Type));
        if (entry == match) {
          return;
        }

        PropertyChangedEventHandler callback = watchedEntryCallbacks.GetValueOrDefault(data);
        if (entry != null && callback != null) {
          entry.PropertyChanged -= callback;
        }

        if (callback == null) {
          callback = (sender, e) => {
            if (e.PropertyName == nameof(ProfileEntryViewModel.Value)) {
              InvokePropertyChanged(property);
            }
          };
          watchedEntryCallbacks[data] = callback;
        }

        if (match != null) {
          match.PropertyChanged += callback;
        }
        entry = match;
        InvokePropertyChanged(property);
      }

      // We're firing events a bit more than we have to here, but ehh
      UpdateWatchedEntry(KnownSettings.GoldenKeysEarned, nameof(GoldenKeys), ref _goldenKeysEarned);
      UpdateWatchedEntry(KnownSettings.keyCount, nameof(GoldenKeys), ref _keyCount);
      UpdateWatchedEntry(KnownSettings.keysSpent, nameof(GoldenKeys), ref _keysSpent);
      InvokePropertyChanged(nameof(MaxGoldenKeys));

      UpdateWatchedEntry(KnownSettings.StashSlot0, nameof(StashSlot0), ref _slashSlot0);
      UpdateWatchedEntry(KnownSettings.StashSlot1, nameof(StashSlot1), ref _slashSlot1);
      UpdateWatchedEntry(KnownSettings.StashSlot2, nameof(StashSlot2), ref _slashSlot2);
      UpdateWatchedEntry(KnownSettings.StashSlot3, nameof(StashSlot3), ref _slashSlot3);

      UpdateWatchedEntry(KnownSettings.BadassPoints, nameof(BadassRank), ref _badassPoints);
      UpdateWatchedEntry(KnownSettings.BadassPointsSpent, nameof(BadassRank), ref _badassPointsSpent);
      UpdateWatchedEntry(KnownSettings.BadassTokens, nameof(BadassTokens), ref _badassTokens);
    }

    private void SetEntryProperty<T>(ProfileEntryViewModel entry, T value, [CallerMemberName]string property = null) {
      if (property == null) {
        throw new ArgumentNullException();
      }
      if (entry != null && value != null && !EqualityComparer<T>.Default.Equals((T)entry.Value, value)) {
        entry.Value = value;
        InvokePropertyChanged(property);
      }
    }
    #endregion

    #region Watched Entries

    #region Golden Keys
    private ProfileEntryViewModel _goldenKeysEarned;
    private ProfileEntryViewModel _keyCount;
    private ProfileEntryViewModel _keysSpent;

    private int? _bl1eKeys => _keyCount == null
                            ? null
                            : (int?)_keyCount.Value - Math.Max(0, (int)(_keysSpent?.Value ?? 0));
    // TODO: does this need caching?
    private int? _bl2Keys {
      get {
        if (_goldenKeysEarned == null) {
          return null;
        }

        byte[] keyBytes = (byte[])_goldenKeysEarned.Value;

        int sum = 0;
        for (int i = 0; i <= (keyBytes.Length - 3); i += 3) {
          sum += keyBytes[i + 1] - keyBytes[i + 2];
        }

        return sum;
      }
    }

    // When we have BL2 style keys, use a conservative max to avoid 9000 byte limit
    public int MaxGoldenKeys => _goldenKeysEarned != null ? 333333 : int.MaxValue;

    public int? GoldenKeys {
      // If we're in BL1E, and we have BL1E style keys, prefer those, otherwise prefer BL2 style keys
      get => DisplayGame == Game.BL1E && _keyCount != null
             ? _bl1eKeys
             : _bl2Keys ?? _bl1eKeys;
      set {
        if (value == null) {
          return;
        }

        // Set both key styles
        bool anyChange = false;
        if (_keyCount != null && value != _bl1eKeys) {
          anyChange = true;
          _keyCount.Value = value + Math.Max(0, (int)(_keysSpent?.Value ?? 0));
        }

        if (_goldenKeysEarned != null && value != _bl2Keys) {
          anyChange = true;

          int keyNum = (int)value;
          byte[] output = new byte[3 * (((keyNum - 1) / 0xFF) + 1)];

          for (int i = 0; i <= (output.Length - 3); i += 3) {
            if (keyNum > 0xFF) {
              output[i + 1] = 0xFF;
              keyNum -= 0xFF;
            } else {
              output[i + 1] = (byte)keyNum;
              break;
            }
          }

          _goldenKeysEarned.Value = output;
        }

        if (anyChange) {
          InvokePropertyChanged();
        }
      }
    }
    #endregion

    #region Stash
    private ProfileEntryViewModel _slashSlot0;
    public byte[] StashSlot0 {
      get => (byte[])_slashSlot0?.Value;
      set => SetEntryProperty(_slashSlot0, value);
    }
    private ProfileEntryViewModel _slashSlot1;
    public byte[] StashSlot1 {
      get => (byte[])_slashSlot1?.Value;
      set => SetEntryProperty(_slashSlot1, value);
    }
    private ProfileEntryViewModel _slashSlot2;
    public byte[] StashSlot2 {
      get => (byte[])_slashSlot2?.Value;
      set => SetEntryProperty(_slashSlot2, value);
    }
    private ProfileEntryViewModel _slashSlot3;
    public byte[] StashSlot3 {
      get => (byte[])_slashSlot3?.Value;
      set => SetEntryProperty(_slashSlot3, value);
    }
    #endregion

    private const int BADASS_POINTS_PER_RANK = 5;
    private ProfileEntryViewModel _badassPoints;
    private ProfileEntryViewModel _badassPointsSpent;
    public int? BadassRank {
      get => _badassPoints == null ? null : (int?)((int)_badassPoints.Value / BADASS_POINTS_PER_RANK);
      set {
        if (value == null) {
          return;
        }
        int rank = (int)value * BADASS_POINTS_PER_RANK;

        bool anyChange = false;
        if (_badassPoints != null && (int)_badassPoints.Value != rank) {
          anyChange = true;
          _badassPoints.Value = rank;
        }
        if (_badassPointsSpent != null && (int)_badassPointsSpent.Value != rank) {
          anyChange = true;
          _badassPointsSpent.Value = rank;
        }

        if (anyChange) {
          InvokePropertyChanged();
        }
      }
    }

    private ProfileEntryViewModel _badassTokens;
    public int? BadassTokens {
      get => (int?)_badassTokens?.Value;
      set => SetEntryProperty(_badassTokens, value);
    }

    #endregion
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
