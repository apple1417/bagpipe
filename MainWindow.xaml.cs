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
using System.Globalization;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.Win32;

namespace bagpipe {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow {
    private Profile profile;

    private OpenFileDialog fileDialog = new OpenFileDialog() {
      Filter = "Profile File|profile.bin;Player.wsg"
    };

    public MainWindow() {
      InitializeComponent();

      profile = new Profile();
      for (int i = 0; i <= 50; i++) {
        profile.Add(new ProfileEntry() {
          Owner = OnlineProfilePropertyOwner.None,
          ID = 123,
          Type = SettingsDataType.String,
          Value = "test",
          AdvertisementType = OnlineDataAdvertisementType.DontAdvertise
        });
      }

      DataContext = new ViewModelObservableCollection<ProfileEntryViewModel, ProfileEntry>(profile, x => new ProfileEntryViewModel(x));
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e) {
      bool? ok = fileDialog.ShowDialog();
      if (ok.HasValue && ok.Value) {
        profile.LoadProfile(fileDialog.FileName);
        // TODO: warnings
      }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) {
      // TODO
    }
  }
}
