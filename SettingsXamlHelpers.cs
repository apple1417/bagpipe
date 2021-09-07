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
        byte[] _ => elem.FindResource("BlobSettingsTemplate") as DataTemplate,
        DateTime _ => elem.FindResource("DateTimeSettingsTemplate") as DataTemplate,
        byte _ => elem.FindResource("ByteSettingsTemplate") as DataTemplate,
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
      return DependencyProperty.UnsetValue;
    }
  }
}
