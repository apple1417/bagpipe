using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace bagpipe {
  class SettingsTypeTemplateSelector : DataTemplateSelector {
    public override DataTemplate SelectTemplate(object item, DependencyObject container) {
      FrameworkElement elem = container as FrameworkElement;
      if (elem == null) {
        return null;
      }
      if (item == null || !(item is ProfileEntryViewModel)) {
        throw new ApplicationException();
      }

      return (item as ProfileEntryViewModel).Type switch {
        SettingsDataType.Empty => elem.FindResource("EmptySettingsTemplate") as DataTemplate,
        SettingsDataType.Int32 => elem.FindResource("Int32SettingsTemplate") as DataTemplate,
        SettingsDataType.Int64 => elem.FindResource("Int64SettingsTemplate") as DataTemplate,
        SettingsDataType.Double => elem.FindResource("DoubleSettingsTemplate") as DataTemplate,
        SettingsDataType.String => elem.FindResource("StringSettingsTemplate") as DataTemplate,
        SettingsDataType.Float => elem.FindResource("FloatSettingsTemplate") as DataTemplate,
        SettingsDataType.Blob => elem.FindResource("BlobSettingsTemplate") as DataTemplate,
        SettingsDataType.DateTime => elem.FindResource("DateTimeSettingsTemplate") as DataTemplate,
        SettingsDataType.Byte => elem.FindResource("ByteSettingsTemplate") as DataTemplate,
        _ => throw new ApplicationException(),
      };
    }
  }

  class CastingConverter : DependencyObject, IValueConverter {
    public Type ResultType {
      get => (Type)GetValue(ResultTypeProperty);
      set => SetValue(ResultTypeProperty, value);
    }
    public static readonly DependencyProperty ResultTypeProperty = DependencyProperty.Register(
      "ResultType", typeof(Type), typeof(CastingConverter), new PropertyMetadata(null)
    );

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return System.Convert.ChangeType(System.Convert.ChangeType(value, (Type)parameter), ResultType);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      return System.Convert.ChangeType(System.Convert.ChangeType(value, ResultType), (Type)parameter);
    }
  }

  class BooleanToZeroWidthConverter : DependencyObject, IValueConverter {
    public double Width {
      get => (double)GetValue(WidthProperty);
      set => SetValue(WidthProperty, value);
    }
    public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
      "Width", typeof(double), typeof(BooleanToZeroWidthConverter), new PropertyMetadata(null)
    );

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return ((bool)value) ? Width : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      Width = (double)value;
      return Binding.DoNothing;
    }
  }

  class HexByteArrayConverter : ValidationRule, IValueConverter {
    // Doesn't seem to be anything built in to check the unicode `Hex_Digit` property
    private static bool IsHexDigit(char c) {
      return (('\u0030' <= c) && (c <= '\u0039'))
             || (('\u0041' <= c) && (c <= '\u0046'))
             || (('\u0061' <= c) && (c <= '\u0066'))
             || (('\uFF10' <= c) && (c <= '\uFF19'))
             || (('\uFF21' <= c) && (c <= '\uFF26'))
             || (('\uFF41' <= c) && (c <= '\uFF46'));
    }

    private static readonly byte?[] nibbleLookup = new byte?[] {
       0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9, null, null, null, null, null, null,
      null,  0xA,  0xB,  0xC,  0xD,  0xE,  0xF, null, null, null, null, null, null, null, null, null,
      null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
      null,  0xA,  0xB,  0xC,  0xD,  0xE,  0xF,
    };

    private static (bool isValid, string errorMsg) TryConvertHexToByteArray(string str, out byte[] output) {
      List<byte> convertedBytes = new List<byte>();

      int? highIdx = null;
      foreach (char c in str) {
        if (char.IsWhiteSpace(c)) {
          continue;
        }
        if (!IsHexDigit(c)) {
          output = new byte[0];
          return (false, $"Invalid hex digit '{c}'!");
        }

        int idx = c - (c > '\u00FF' ? '\uFF10' : '\x0030');
        if (highIdx.HasValue) {
          convertedBytes.Add((byte)((nibbleLookup[highIdx.Value] << 4) | nibbleLookup[idx]));
          highIdx = null;
        } else {
          highIdx = idx;
        }
      }

      if (highIdx.HasValue) {
        output = new byte[0];
        return (false, "Odd number of hex digits!");
      }

      output = convertedBytes.ToArray();
      return (true, null);
    }

    public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
      (bool isValid, string errorMsg) = TryConvertHexToByteArray((string) value, out _);
      return isValid ? ValidationResult.ValidResult : new ValidationResult(false, errorMsg);
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return BitConverter.ToString((byte[])value).Replace("-", " ");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      (bool isValid, _) = TryConvertHexToByteArray((string)value, out byte[] converted);
      return isValid ? converted : DependencyProperty.UnsetValue;
    }
  }
}
