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

    public MainWindow() {
      InitializeComponent();

      Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name);

      ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
      ThemeManager.Current.SyncTheme();

      profile = new Profile();
      DataContext = new ProfileViewModel(profile);
    }

    #region File Handling
    private readonly OpenFileDialog openDialog = new OpenFileDialog() {
      Filter = "Profile Files|profile.bin;Player.wsg|All Files (*.*)|*.*"
    };
    private readonly SaveFileDialog saveDialog = new SaveFileDialog() {
      Filter = "Profile Files|profile.bin;Player.wsg"
    };

    private void OpenButton_Click(object sender, RoutedEventArgs e) {
      openDialog.FileName = "";
      bool? ok = openDialog.ShowDialog();
      if (ok.HasValue && ok.Value) {
        // TODO: processing dialog
        bool warn = profile.Load(openDialog.FileName);
        if (warn) {
          _ = this.ShowMessageAsync(
            "Warning",
            "Unexpected data was encountered while loading the profile. This may have caused some values to be intepreted incorrectly."
          );
        }
      }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e) {
      saveDialog.FileName = "";
      bool? ok = saveDialog.ShowDialog();
      if (ok.HasValue && ok.Value) {
        // TODO: processing dialog

        if (profile.IsOverSizeLimit()) {
          MessageDialogResult res = await this.ShowMessageAsync(
            "Warning",
            "This profile contains over 9000 bytes of raw data, which may make the game read it as corrupt. Do you want to continue?",
            MessageDialogStyle.AffirmativeAndNegative,
            new MetroDialogSettings() {
              AffirmativeButtonText = "Yes",
              NegativeButtonText = "No"
            }
          );
          if (res != MessageDialogResult.Affirmative) {
            return;
          }
        }

        profile.Save(saveDialog.FileName);
      }
    }
    #endregion

    #region Entry Manipulation
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
    #endregion

    private void UnlockCustomizations_Click(object sender, RoutedEventArgs e) => ((ProfileViewModel)DataContext).UpdateCustomizations(true);
    private void LockCustomizations_Click(object sender, RoutedEventArgs e) => ((ProfileViewModel)DataContext).UpdateCustomizations(false);
  }
}
