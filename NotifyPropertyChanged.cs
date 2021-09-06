using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace bagpipe {
  class NotifyPropertyChanged : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName]string property = null) {
      if (property == null) {
        throw new ArgumentNullException();
      }
      if (!EqualityComparer<T>.Default.Equals(field, value)) {
        field = value;
        InvokePropertyChanged(property);
      }
    }

    protected void InvokePropertyChanged([CallerMemberName]string property = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
  }
}
