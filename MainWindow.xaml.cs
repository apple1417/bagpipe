using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Globalization;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using ControlzEx.Theming;
using System.Windows.Markup;

namespace bagpipe {
  public partial class MainWindow : MetroWindow {
    private readonly Profile profile;

    private readonly OpenFileDialog openDialog = new OpenFileDialog() {
      Filter = "Profile Files|profile.bin;Player.wsg|All Files (*.*)|*.*"
    };
    private readonly SaveFileDialog saveDialog = new SaveFileDialog() {
      Filter = "Profile Files|profile.bin;Player.wsg"
    };

    public MainWindow() {
      InitializeComponent();

      Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name);

      ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
      ThemeManager.Current.SyncTheme();

      profile = new Profile();
      DataContext = new ProfileViewModel(profile);
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e) {
      bool? ok = openDialog.ShowDialog();
      if (ok.HasValue && ok.Value) {
        bool warn = profile.Load(openDialog.FileName);
        if (warn) {
          _ = this.ShowMessageAsync(
            "Warning",
            "Unexpected data was encountered while loading the profile. This may have caused some values to be intepreted incorrectly."
          );
        }
      }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) {
      bool? ok = saveDialog.ShowDialog();
      if (ok.HasValue && ok.Value) {
        bool warn = profile.Save(saveDialog.FileName);
        if (warn) {
          _ = this.ShowMessageAsync(
            "Warning",
            "Unexpected data was encountered while saving the profile. This may have caused some values to be written incorrectly."
          );
        }
      }
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e) {
      NewEntryDialog dialog = new NewEntryDialog(((ProfileViewModel)DataContext).DisplayGame);
      await this.ShowMetroDialogAsync(dialog);

      ProfileEntry entry = await dialog.GetCreatedEntry();
      if (!(entry is null)) {
        profile.Entries.Add(entry);
      }

      await this.HideMetroDialogAsync(dialog);
    }

    private bool DeleteRawEntries() {
      if (AdvancedSwitch.IsOn) {
        IEnumerable<int> selectedIndexes = RawListView.SelectedItems.Cast<ProfileEntryViewModel>().Select(x => RawListView.Items.IndexOf(x));
        foreach (int idx in selectedIndexes.OrderByDescending(x => x)) {
          profile.Entries.RemoveAt(idx);
        }
        return true;
      }
      return false;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e) {
      DeleteRawEntries();
    }

    private void RawListView_KeyDown(object sender, KeyEventArgs e) {
      if (e.Key == Key.Delete) {
        e.Handled |= DeleteRawEntries();
      }
    }
  }
}
