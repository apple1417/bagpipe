using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace bagpipe {
  class ViewModelBase : INotifyDataErrorInfo, INotifyPropertyChanged {
    public bool HasErrors => knownErrors.Any();

    public event PropertyChangedEventHandler PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    private Dictionary<string, List<string>> knownErrors = new Dictionary<string, List<string>>();

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
      if (property == null) {
        throw new ArgumentNullException();
      }
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }

    public IEnumerable GetErrors(string property) {
      if (property == null) {
        throw new ArgumentNullException();
      }
      return knownErrors.GetValueOrDefault(property);
    }

    protected bool PropertyValid([CallerMemberName]string property = null) {
      if (property == null) {
        throw new ArgumentNullException();
      }
      return !(knownErrors.GetValueOrDefault(property)?.Any() ?? false);
    }

    protected void ClearErrors([CallerMemberName]string property = null) {
      if (property == null) {
        throw new ArgumentNullException();
      }
      knownErrors.Remove(property);
    }

    protected void ValidationCheck(bool isValid, string msg, [CallerMemberName]string property = null) {
      if (property == null) {
        throw new ArgumentNullException();
      }
      if (isValid) {
        if (knownErrors.ContainsKey(property)) {
          knownErrors[property].Remove(msg);
          InvokeErrorsChanged(property);
        }
      } else {
        if (!knownErrors.ContainsKey(property)) {
          knownErrors[property] = new List<string>();
        }
        if (!knownErrors[property].Contains(msg)) {
          knownErrors[property].Add(msg);
          InvokeErrorsChanged(property);
        }
      }
    }

    protected void InvokeErrorsChanged([CallerMemberName]string property = null) {
      if (property == null) {
        throw new ArgumentNullException();
      }
      ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
      InvokePropertyChanged(nameof(HasErrors));
    }
  }
}
