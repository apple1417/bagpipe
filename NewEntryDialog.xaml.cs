using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;

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
      DataContext = new NewEntryViewModel(entry) { DisplayGame = DisplayGame };
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
    private readonly ProfileEntry entry;
    public NewEntryViewModel(ProfileEntry entry) {
      this.entry = entry;
    }

    public int ID {
      get => entry.ID;
      set => SetProperty(ref entry.ID, value);
    }

    public SettingsDataType Type {
      get => entry.Type;
      set {
        SetProperty(ref entry.Type, value);
        entry.Value = value switch {
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
      }
    }

    private Game _game;
    public Game DisplayGame {
      get => _game;
      set => SetProperty(ref _game, value);
    }
  }
}
