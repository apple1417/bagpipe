using System;
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

      return (item as ProfileEntryViewModel).Value switch {
        null => elem.FindResource("EmptySettingsTemplate") as DataTemplate,
        int _ => elem.FindResource("Int32SettingsTemplate") as DataTemplate,
        long _ => elem.FindResource("Int64SettingsTemplate") as DataTemplate,
        double _ => elem.FindResource("DoubleSettingsTemplate") as DataTemplate,
        string _ => elem.FindResource("StringSettingsTemplate") as DataTemplate,
        float _ => elem.FindResource("FloatSettingsTemplate") as DataTemplate,
        // TODO
        // byte[] _ => elem.FindResource("BlobSettingsTemplate") as DataTemplate,
        DateTime _ => elem.FindResource("DateTimeSettingsTemplate") as DataTemplate,
        byte _ => elem.FindResource("ByteSettingsTemplate") as DataTemplate,
        _ => throw new ApplicationException(),
      };
    }
  }

  class CastingConverter<T, U> : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return System.Convert.ChangeType((T)value, typeof(U));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      return System.Convert.ChangeType((U)value, typeof(T));
    }
  }
  class Int32SettingConverter : CastingConverter<int, double> { }
  class Int64SettingConverter : CastingConverter<long, double> { }
  class FloatSettingConverter : CastingConverter<float, double> { }
  class ByteSettingConverter : CastingConverter<byte, double> { }
}
