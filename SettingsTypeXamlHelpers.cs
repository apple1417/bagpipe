using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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

      switch ((item as ProfileEntryViewModel).Type) {
        case SettingsDataType.Empty:
          return elem.FindResource("EmptySettingsTemplate") as DataTemplate;
        case SettingsDataType.Int32:
          return elem.FindResource("Int32SettingsTemplate") as DataTemplate;
        case SettingsDataType.Int64:
          return elem.FindResource("Int64SettingsTemplate") as DataTemplate;
        case SettingsDataType.Double:
          return elem.FindResource("DoubleSettingsTemplate") as DataTemplate;
        case SettingsDataType.String:
          return elem.FindResource("StringSettingsTemplate") as DataTemplate;
        case SettingsDataType.Float:
          return elem.FindResource("FloatSettingsTemplate") as DataTemplate;
        /* TODO
        case SettingsDataType.Blob:
          return elem.FindResource("BlobSettingsTemplate") as DataTemplate;*/
        case SettingsDataType.DateTime:
          return elem.FindResource("DateTimeSettingsTemplate") as DataTemplate;
        case SettingsDataType.Byte:
          return elem.FindResource("ByteSettingsTemplate") as DataTemplate;
        default:
          throw new ApplicationException();
      }
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
  class Int32SettingConverter : CastingConverter<Int32, Double> { }
  class Int64SettingConverter : CastingConverter<Int64, Double> { }
  class FloatSettingConverter : CastingConverter<Single, Double> { }
  class ByteSettingConverter : CastingConverter<Byte, Double> { }
}
