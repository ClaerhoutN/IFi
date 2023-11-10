using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IFi.Utilities.Collections.ObjectModel
{
    public class SilentObservableCollection<T> : ObservableCollection<T>
    {
        public void AddRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            int startIndex = Count;
            foreach (var item in items)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(items), startIndex));
            OnCountPropertyChanged();
            OnIndexerPropertyChanged();
        }
        public void ResetRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            if(Items == items || items is not IList<T>)
            {
                items = items.ToList(); //create a shallow copy to prevent clearing the destination collection
            }
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(items), 0));
            OnCountPropertyChanged();
            OnIndexerPropertyChanged();
            OnCollectionReset();
        }

        private void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);
        private void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
        private void OnCollectionReset() => OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
        internal static class EventArgsCache
        {
            internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");
            internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
            internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
        }
    }
}
