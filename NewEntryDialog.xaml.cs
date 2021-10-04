using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace bagpipe {
  public partial class NewEntryDialog : CustomDialog {
    private ProfileEntry entry;

    private TaskCompletionSource<ProfileEntry> tcs;
    internal Task<ProfileEntry> GetCreatedEntry() => tcs.Task;

    internal NewEntryDialog(Game DisplayGame) : base(null, null) {
      InitializeComponent();

      Title = "New Profile Entry";

      tcs = new TaskCompletionSource<ProfileEntry>();

      entry = new ProfileEntry() {
        Owner = OnlineProfilePropertyOwner.Game,
        ID = 0,
        Type = SettingsDataType.Int32,
        Value = 0,
        AdvertisementType = OnlineDataAdvertisementType.DontAdvertise
      };
      DataContext = new NewEntryViewModel(entry, DisplayGame, PresetComboBox);
    }

    private void Add() => tcs.TrySetResult(entry);
    private void Cancel() => tcs.TrySetResult(null);

    private void AddButton_Click(object sender, RoutedEventArgs e) {
      Add();
      e.Handled = true;
    }
    private void AddButton_KeyDown(object sender, KeyEventArgs e) {
      if (e.Key == Key.Enter) {
        Add();
        e.Handled = true;
      }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) {
      Cancel();
      e.Handled = true;
    }
    private void CancelButton_KeyDown(object sender, KeyEventArgs e) {
      if (e.Key == Key.Enter) {
        Cancel();
        e.Handled = true;
      }
    }

    private void MainForm_KeyDown(object sender, KeyEventArgs e) {
      if (e.Key == Key.Escape) {
        Cancel();
        e.Handled = true;
      }
    }
  }

  class NewEntryViewModel: ViewModelBase {
    public static ReadOnlyObservableCollection<Game> ValidGames { get; } = new ReadOnlyObservableCollection<Game>(
      new ObservableCollection<Game>(
        Enum.GetValues(typeof(Game)).Cast<Game>().Where(x => x != Game.None)
      )
    );

    private readonly ProfileEntry Entry;
    private ComboBox PresetComboBox;
    public NewEntryViewModel(ProfileEntry Entry, Game DisplayGame, ComboBox PresetComboBox) {
      this.Entry = Entry;
      this.PresetComboBox = PresetComboBox;

      // Default to TPS cause it has the most known fields
      this.DisplayGame = DisplayGame == Game.None ? Game.TPS : DisplayGame;

      PresetComboBox.SelectionChanged += PresetComboBox_SelectionChanged;
    }

    private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      KnownSettingInfo selected = (KnownSettingInfo)PresetComboBox.SelectedItem;
      if (selected != null) {
        ID = selected.ID;
        Type = selected.Type;
      }
    }

    private void TryUpdatePreset() {
      KnownSettingInfo match = KnownSettings.Data.GetValueOrDefault(DisplayGame)?.GetValueOrDefault(ID);
      if (match != null && match.Type == Type && Presets.Contains(match)) {
        PresetComboBox.SelectedItem = match;
      } else {
        PresetComboBox.SelectedItem = null;
      }
    }

    public int ID {
      get => Entry.ID;
      set {
        SetProperty(ref Entry.ID, value);
        TryUpdatePreset();
      }
    }

    public SettingsDataType Type {
      get => Entry.Type;
      set {
        SetProperty(ref Entry.Type, value);
        Entry.Value = value switch {
          SettingsDataType.Empty => null,
          SettingsDataType.Int32 => 0,
          SettingsDataType.Int64 => 0L,
          SettingsDataType.Double => 0.0d,
          SettingsDataType.String => "",
          SettingsDataType.Float => 0.0f,
          SettingsDataType.Blob => new byte[0],
          SettingsDataType.DateTime => DateTime.Now,
          SettingsDataType.Byte => (byte)0,
          _ => throw new NotImplementedException(),
        };
        TryUpdatePreset();
      }
    }

    private static readonly IEnumerable<KnownSettingInfo> _emptyList = new List<KnownSettingInfo>();
    public ReadOnlyObservableCollection<KnownSettingInfo> Presets {
      get => new ReadOnlyObservableCollection<KnownSettingInfo>(
        new ObservableCollection<KnownSettingInfo>(
          KnownSettings.Data.GetValueOrDefault(DisplayGame)?.Values ?? _emptyList
        )
      );
    }

    private Game _game;
    public Game DisplayGame {
      get => _game;
      set {
        if (value != Game.None && value != _game) {
          _game = value;
          InvokePropertyChanged(nameof(DisplayGame));
          InvokePropertyChanged(nameof(Presets));
          PresetComboBox.SelectedItem = null;
        }
      }
    }
  }
}
