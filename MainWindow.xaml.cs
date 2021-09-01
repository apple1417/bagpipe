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

namespace bagpipe {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow {
    private List<ProfileEntry> entries;

    public MainWindow() {
      InitializeComponent();

      entries = new List<ProfileEntry>() {
        new ProfileEntry() {
          Owner = OnlineProfilePropertyOwner.None,
          ID = 123,
          Type = SettingsDataType.String,
          Value = "test",
          AdvertisementType = OnlineDataAdvertisementType.DontAdvertise
        },
        new ProfileEntry() {
          Owner = OnlineProfilePropertyOwner.None,
          ID = 456,
          Type = SettingsDataType.Int32,
          Value = 1234,
          AdvertisementType = OnlineDataAdvertisementType.DontAdvertise
        },
        new ProfileEntry() {
          Owner = OnlineProfilePropertyOwner.None,
          ID = 456,
          Type = SettingsDataType.Double,
          Value = 1234.3,
          AdvertisementType = OnlineDataAdvertisementType.DontAdvertise
        }
      };

      DataContext = entries.Select(x => new ProfileEntryViewModel(x));
    }
  }
}
