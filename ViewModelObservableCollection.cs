using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace bagpipe {
  class ViewModelObservableCollection<TViewModel, TModel> : ObservableCollection<TViewModel> {
    private readonly ObservableCollection<TModel> source;
    private readonly Func<TModel, TViewModel> factory;

    public ViewModelObservableCollection(
      ObservableCollection<TModel> source,
      Func<TModel, TViewModel> factory
    ) : base(source.Select(x => factory(x))) {

      this.source = source;
      this.factory = factory;

      this.source.CollectionChanged += Source_CollectionChanged;
    }

    private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      void AddNewItems() {
        for (int i = 0; i < e.NewItems.Count; i++) {
          Insert(e.NewStartingIndex + i, factory((TModel)e.NewItems[i]));
        }
      }
      void RemoveOldItems() {
        for (int i = 0; i < e.OldItems.Count; i++) {
          RemoveAt(e.OldStartingIndex);
        }
      }

      switch (e.Action) {
        case NotifyCollectionChangedAction.Add: {
          AddNewItems();
          break;
        }
        case NotifyCollectionChangedAction.Remove: {
          RemoveOldItems();
          break;
        }
        case NotifyCollectionChangedAction.Replace: {
          RemoveOldItems();
          AddNewItems();
          break;
        }
        case NotifyCollectionChangedAction.Move: {
          List<TViewModel> items = this.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();

          RemoveOldItems();
          // Can't adapt AddNewItems since we don't want to use the factory
          for (int i = 0; i < items.Count; i++) {
            Insert(e.NewStartingIndex + i, items[i]);
          }
          break;
        }
        case NotifyCollectionChangedAction.Reset: {
          Clear();
          AddNewItems();
          break;
        }

        default:
          break;
      }
    }
  }
}
