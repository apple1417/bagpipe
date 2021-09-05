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

namespace bagpipe {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow {
    private readonly Profile profile;

    private readonly OpenFileDialog fileDialog = new OpenFileDialog() {
      Filter = "Profile File|profile.bin;Player.wsg"
    };

    public MainWindow() {
      InitializeComponent();

      profile = new Profile();
      DataContext = new ProfileViewModel(profile);
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e) {
      bool? ok = fileDialog.ShowDialog();
      if (ok.HasValue && ok.Value) {
        bool warn = profile.Load(fileDialog.FileName);
        if (warn) {
          _ = this.ShowMessageAsync(
            "Warning",
            "Unexpected data was encountered while loading the profile. This may have caused some values to be intepreted incorrectly."
          );
        }
      }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) {
      // TODO
    }
  }
}
