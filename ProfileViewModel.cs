using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace bagpipe {
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

    #region Watched Entry Updates
    private Dictionary<KnownSettingInfo, PropertyChangedEventHandler> watchedEntryCallbacks = new Dictionary<KnownSettingInfo, PropertyChangedEventHandler>();

    private void UpdateWatchedEntries() {
      void UpdateWatchedEntry(KnownSettingInfo known, ref ProfileEntryViewModel entry, string property) {
        ProfileEntryViewModel match = Entries.FirstOrDefault(x => known.Matches(x.ID, x.Type));
        if (entry == match) {
          return;
        }

        PropertyChangedEventHandler callback = watchedEntryCallbacks.GetValueOrDefault(known);
        if (entry != null && callback != null) {
          entry.PropertyChanged -= callback;
        }

        if (callback == null) {
          callback = (sender, e) => {
            if (e.PropertyName == nameof(ProfileEntryViewModel.Value)) {
              InvokePropertyChanged(property);
            }
          };
          watchedEntryCallbacks[known] = callback;
        }

        if (match != null) {
          match.PropertyChanged += callback;
        }
        entry = match;
        InvokePropertyChanged(property);
      }

      InvokePropertyChanged(nameof(HasCustomizations));

      // We're firing events a bit more than we have to here, but ehh
      UpdateWatchedEntry(KnownSettings.GoldenKeysEarned, ref _goldenKeysEarned, nameof(GoldenKeys));
      UpdateWatchedEntry(KnownSettings.keyCount, ref _keyCount, nameof(GoldenKeys));
      UpdateWatchedEntry(KnownSettings.keysSpent, ref _keysSpent, nameof(GoldenKeys));
      InvokePropertyChanged(nameof(MaxGoldenKeys));

      UpdateWatchedEntry(KnownSettings.StashSlot0, ref _slashSlot0, nameof(StashSlot0));
      UpdateWatchedEntry(KnownSettings.StashSlot1, ref _slashSlot1, nameof(StashSlot1));
      UpdateWatchedEntry(KnownSettings.StashSlot2, ref _slashSlot2, nameof(StashSlot2));
      UpdateWatchedEntry(KnownSettings.StashSlot3, ref _slashSlot3, nameof(StashSlot3));

      UpdateWatchedEntry(KnownSettings.BadassPoints, ref _badassPoints, nameof(BadassRank));
      UpdateWatchedEntry(KnownSettings.BadassPointsSpent, ref _badassPointsSpent, nameof(BadassRank));
      UpdateWatchedEntry(KnownSettings.BadassTokens, ref _badassTokens, nameof(BadassTokens));

      // This is similar, but different enough we can't really use the function
      ProfileEntryViewModel rewardsMatch = Entries.FirstOrDefault(x => KnownSettings.BadassRewardsEarned.Matches(x.ID, x.Type));
      if (_badassRewardsEntry != rewardsMatch) {
        badassRewards = new BARRewards(rewardsMatch);

        PropertyChangedEventHandler callback = watchedEntryCallbacks.GetValueOrDefault(KnownSettings.BadassRewardsEarned);
        if (_badassRewardsEntry != null && callback != null) {
          _badassRewardsEntry.PropertyChanged -= callback;
        }

        if (callback == null) {
          callback = (sender, e) => {
            // We don't need all our values to throw changed events when we're only updating one
            if (e.PropertyName == nameof(ProfileEntryViewModel.Value) && !badassRewards.InUpdate) {
              foreach (string property in BADASS_REWARD_PROPERTIES) {
                InvokePropertyChanged(property);
              }
            }
          };
          watchedEntryCallbacks[KnownSettings.BadassRewardsEarned] = callback;
        }

        if (rewardsMatch != null) {
          rewardsMatch.PropertyChanged += callback;
        }
        _badassRewardsEntry = rewardsMatch;
        foreach (string property in BADASS_REWARD_PROPERTIES) {
          InvokePropertyChanged(property);
        }
      }

      InvokePropertyChanged(nameof(HasBarRewards));
    }

    private void SetEntryProperty<T>(ProfileEntryViewModel entry, T value, [CallerMemberName] string property = null) {
      if (property == null) {
        throw new ArgumentNullException();
      }
      if (entry != null && value != null && !EqualityComparer<T>.Default.Equals((T)entry.Value, value)) {
        entry.Value = value;
        InvokePropertyChanged(property);
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

    #region Golden Keys
    private ProfileEntryViewModel _goldenKeysEarned;
    private ProfileEntryViewModel _keyCount;
    private ProfileEntryViewModel _keysSpent;

    private int? _bl1eKeys => _keyCount == null
                            ? null
                            : (int?)_keyCount.Value - Math.Max(0, (int)(_keysSpent?.Value ?? 0));
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

    #region BAR Rewards
    private ProfileEntryViewModel _badassRewardsEntry;
    private BARRewards badassRewards;

    public bool HasBarRewards => _badassRewardsEntry != null;

    private readonly string[] BADASS_REWARD_PROPERTIES = new string[] {
      nameof(CritDamage),
      nameof(CritDamageInterval),
      nameof(ElementalChance),
      nameof(ElementalChanceInterval),
      nameof(ElementalDamage),
      nameof(ElementalDamageInterval),
      nameof(FireRate),
      nameof(FireRateInterval),
      nameof(GrenadeDamage),
      nameof(GrenadeDamageInterval),
      nameof(GunAccuracy),
      nameof(GunAccuracyInterval),
      nameof(GunDamage),
      nameof(GunDamageInterval),
      nameof(MaxHealth),
      nameof(MaxHealthInterval),
      nameof(MeleeDamage),
      nameof(MeleeDamageInterval),
      nameof(RecoilReduction),
      nameof(RecoilReductionInterval),
      nameof(ReloadSpeed),
      nameof(ReloadSpeedInterval),
      nameof(ShieldCapacity),
      nameof(ShieldCapacityInterval),
      nameof(ShieldDelay),
      nameof(ShieldDelayInterval),
      nameof(ShieldRate),
      nameof(ShieldRateInterval)
    };

    public double? CritDamage {
      get => badassRewards?[BARRewardStat.CritDamage];
      set {
        if (value != badassRewards[BARRewardStat.CritDamage]) {
          badassRewards[BARRewardStat.CritDamage] = value;
          InvokePropertyChanged(nameof(CritDamage));
          InvokePropertyChanged(nameof(CritDamageInterval));
        }
      }
    }
    public double CritDamageInterval => badassRewards?.GetInterval(BARRewardStat.CritDamage) ?? 1;

    public double? ElementalChance {
      get => badassRewards?[BARRewardStat.ElementalChance];
      set {
        if (value != badassRewards[BARRewardStat.ElementalChance]) {
          badassRewards[BARRewardStat.ElementalChance] = value;
          InvokePropertyChanged(nameof(ElementalChance));
          InvokePropertyChanged(nameof(ElementalChanceInterval));
        }
      }
    }
    public double ElementalChanceInterval => badassRewards?.GetInterval(BARRewardStat.ElementalChance) ?? 1;

    public double? ElementalDamage {
      get => badassRewards?[BARRewardStat.ElementalDamage];
      set {
        if (value != badassRewards[BARRewardStat.ElementalDamage]) {
          badassRewards[BARRewardStat.ElementalDamage] = value;
          InvokePropertyChanged(nameof(ElementalDamage));
          InvokePropertyChanged(nameof(ElementalDamageInterval));
        }
      }
    }
    public double ElementalDamageInterval => badassRewards?.GetInterval(BARRewardStat.ElementalDamage) ?? 1;

    public double? FireRate {
      get => badassRewards?[BARRewardStat.FireRate];
      set {
        if (value != badassRewards[BARRewardStat.FireRate]) {
          badassRewards[BARRewardStat.FireRate] = value;
          InvokePropertyChanged(nameof(FireRate));
          InvokePropertyChanged(nameof(FireRateInterval));
        }
      }
    }
    public double FireRateInterval => badassRewards?.GetInterval(BARRewardStat.FireRate) ?? 1;

    public double? GrenadeDamage {
      get => badassRewards?[BARRewardStat.GrenadeDamage];
      set {
        if (value != badassRewards[BARRewardStat.GrenadeDamage]) {
          badassRewards[BARRewardStat.GrenadeDamage] = value;
          InvokePropertyChanged(nameof(GrenadeDamage));
          InvokePropertyChanged(nameof(GrenadeDamageInterval));
        }
      }
    }
    public double GrenadeDamageInterval => badassRewards?.GetInterval(BARRewardStat.GrenadeDamage) ?? 1;

    public double? GunAccuracy {
      get => badassRewards?[BARRewardStat.GunAccuracy];
      set {
        if (value != badassRewards[BARRewardStat.GunAccuracy]) {
          badassRewards[BARRewardStat.GunAccuracy] = value;
          InvokePropertyChanged(nameof(GunAccuracy));
          InvokePropertyChanged(nameof(GunAccuracyInterval));
        }
      }
    }
    public double GunAccuracyInterval => badassRewards?.GetInterval(BARRewardStat.GunAccuracy) ?? 1;

    public double? GunDamage {
      get => badassRewards?[BARRewardStat.GunDamage];
      set {
        if (value != badassRewards[BARRewardStat.GunDamage]) {
          badassRewards[BARRewardStat.GunDamage] = value;
          InvokePropertyChanged(nameof(GunDamage));
          InvokePropertyChanged(nameof(GunDamageInterval));
        }
      }
    }
    public double GunDamageInterval => badassRewards?.GetInterval(BARRewardStat.GunDamage) ?? 1;

    public double? MaxHealth {
      get => badassRewards?[BARRewardStat.MaxHealth];
      set {
        if (value != badassRewards[BARRewardStat.MaxHealth]) {
          badassRewards[BARRewardStat.MaxHealth] = value;
          InvokePropertyChanged(nameof(MaxHealth));
          InvokePropertyChanged(nameof(MaxHealthInterval));
        }
      }
    }
    public double MaxHealthInterval => badassRewards?.GetInterval(BARRewardStat.MaxHealth) ?? 1;

    public double? MeleeDamage {
      get => badassRewards?[BARRewardStat.MeleeDamage];
      set {
        if (value != badassRewards[BARRewardStat.MeleeDamage]) {
          badassRewards[BARRewardStat.MeleeDamage] = value;
          InvokePropertyChanged(nameof(MeleeDamage));
          InvokePropertyChanged(nameof(MeleeDamageInterval));
        }
      }
    }
    public double MeleeDamageInterval => badassRewards?.GetInterval(BARRewardStat.MeleeDamage) ?? 1;

    public double? RecoilReduction {
      get => badassRewards?[BARRewardStat.RecoilReduction];
      set {
        if (value != badassRewards[BARRewardStat.RecoilReduction]) {
          badassRewards[BARRewardStat.RecoilReduction] = value;
          InvokePropertyChanged(nameof(RecoilReduction));
          InvokePropertyChanged(nameof(RecoilReductionInterval));
        }
      }
    }
    public double RecoilReductionInterval => badassRewards?.GetInterval(BARRewardStat.RecoilReduction) ?? 1;

    public double? ReloadSpeed {
      get => badassRewards?[BARRewardStat.ReloadSpeed];
      set {
        if (value != badassRewards[BARRewardStat.ReloadSpeed]) {
          badassRewards[BARRewardStat.ReloadSpeed] = value;
          InvokePropertyChanged(nameof(ReloadSpeed));
          InvokePropertyChanged(nameof(ReloadSpeedInterval));
        }
      }
    }
    public double ReloadSpeedInterval => badassRewards?.GetInterval(BARRewardStat.ReloadSpeed) ?? 1;

    public double? ShieldCapacity {
      get => badassRewards?[BARRewardStat.ShieldCapacity];
      set {
        if (value != badassRewards[BARRewardStat.ShieldCapacity]) {
          badassRewards[BARRewardStat.ShieldCapacity] = value;
          InvokePropertyChanged(nameof(ShieldCapacity));
          InvokePropertyChanged(nameof(ShieldCapacityInterval));
        }
      }
    }
    public double ShieldCapacityInterval => badassRewards?.GetInterval(BARRewardStat.ShieldCapacity) ?? 1;

    public double? ShieldDelay {
      get => badassRewards?[BARRewardStat.ShieldDelay];
      set {
        if (value != badassRewards[BARRewardStat.ShieldDelay]) {
          badassRewards[BARRewardStat.ShieldDelay] = value;
          InvokePropertyChanged(nameof(ShieldDelay));
          InvokePropertyChanged(nameof(ShieldDelayInterval));
        }
      }
    }
    public double ShieldDelayInterval => badassRewards?.GetInterval(BARRewardStat.ShieldDelay) ?? 1;

    public double? ShieldRate {
      get => badassRewards?[BARRewardStat.ShieldRate];
      set {
        if (value != badassRewards[BARRewardStat.ShieldRate]) {
          badassRewards[BARRewardStat.ShieldRate] = value;
          InvokePropertyChanged(nameof(ShieldRate));
          InvokePropertyChanged(nameof(ShieldRateInterval));
        }
      }
    }
    public double ShieldRateInterval => badassRewards?.GetInterval(BARRewardStat.ShieldRate) ?? 1;
    #endregion
  }
}
