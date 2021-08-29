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

namespace bagpipe {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : MetroWindow {
    public MainWindow() {
      InitializeComponent();

      var entries = new List<(int id, object val)>() {
        (123, 456),
        (5423, 456.789),
        (12, "abcasdas"),
        (123, 456),
        (5423, 456.789),
        (12, "abcasdas"),
        (123, 456),
        (5423, 456.789),
        (12, "abcasdas"),
        (123, 456),
        (5423, 456.789),
        (12, "abcasdas"),
        (123, 456),
        (5423, 456.789),
        (12, "abcasdas"),
        (123, 456),
        (5423, 456.789),
        (12, "abcasdas"),
      };

      foreach (var entry in entries) {
        AddEntry(entry.id, entry.val);
      }
    }

    public void AddEntry(int id, object val) {
      Control valueControl;
      if (val is int intVal) {
        valueControl = new NumericUpDown() {
          Minimum = int.MinValue,
          Maximum = int.MaxValue,
          Value = intVal,
          NumericInputMode = NumericInput.Numbers,
          ParsingNumberStyle = NumberStyles.Integer | NumberStyles.AllowThousands
        };
      } else if (val is long longVal) {
        valueControl = new NumericUpDown() {
          Minimum = long.MinValue,
          Maximum = long.MaxValue,
          Value = longVal,
          NumericInputMode = NumericInput.Numbers,
          ParsingNumberStyle = NumberStyles.Integer | NumberStyles.AllowThousands
        };
      } else if (val is double doubleVal) {
        valueControl = new NumericUpDown() {
          Minimum = double.MinValue,
          Maximum = double.MaxValue,
          Value = doubleVal,
          NumericInputMode = NumericInput.Decimal,
          ParsingNumberStyle = NumberStyles.Number
        };
      } else if (val is string strVal) {
        valueControl = new TextBox() {
          Text = strVal,
        };
      } else if (val is float floatVal) {
        valueControl = new NumericUpDown() {
          Minimum = float.MinValue,
          Maximum = float.MaxValue,
          Value = floatVal,
          NumericInputMode = NumericInput.Decimal,
          ParsingNumberStyle = NumberStyles.Number
        };
      } else if (val is byte[] blobVal) {
        // TODO
        throw new ArgumentException("Can't handle byte arrays");
      } else if (val is DateTime timeVal) {
        valueControl = new DateTimePicker() {
          DisplayDate = timeVal
        };
      } else if (val is byte byteVal) {
        valueControl = new NumericUpDown() {
          Minimum = byte.MinValue,
          Maximum = byte.MaxValue,
          Value = byteVal,
          NumericInputMode = NumericInput.Numbers,
          ParsingNumberStyle = NumberStyles.Integer
        };
      } else {
        throw new ArgumentException($"Tried to add field of invalid type {val.GetType()}");
      }

      rawGrid.RowDefinitions.Add(new RowDefinition());

      Label label = new Label() { Content = $"Unknown ID {id}" };
      rawGrid.Children.Add(label);
      Grid.SetRow(label, rawGrid.RowDefinitions.Count - 1);
      Grid.SetColumn(label, 0);

      rawGrid.Children.Add(valueControl);
      Grid.SetRow(valueControl, rawGrid.RowDefinitions.Count - 1);
      Grid.SetColumn(valueControl, 1);
    }
  }
}
