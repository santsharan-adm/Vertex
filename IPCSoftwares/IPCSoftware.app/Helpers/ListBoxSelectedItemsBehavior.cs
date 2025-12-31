using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Helpers
{
    public static class ListBoxSelectedItemsBehavior
    {
        public static readonly DependencyProperty BindableSelectedItemsProperty =
  DependencyProperty.RegisterAttached(
        "BindableSelectedItems",
    typeof(IList),
         typeof(ListBoxSelectedItemsBehavior),
            new PropertyMetadata(null, OnBindableSelectedItemsChanged));

 public static void SetBindableSelectedItems(DependencyObject element, IList value)
    {
            element.SetValue(BindableSelectedItemsProperty, value);
        }

  public static IList GetBindableSelectedItems(DependencyObject element)
        {
   return (IList)element.GetValue(BindableSelectedItemsProperty);
      }

        private static void OnBindableSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
 {
            if (d is ListBox listBox)
            {
        // Unsubscribe from old collection
          if (e.OldValue is INotifyCollectionChanged oldCollection)
 {
             oldCollection.CollectionChanged -= (s, args) => OnCollectionChanged(listBox, args);
      }

              // Subscribe to new collection
     if (e.NewValue is INotifyCollectionChanged newCollection)
        {
        newCollection.CollectionChanged += (s, args) => OnCollectionChanged(listBox, args);
        }

          listBox.SelectionChanged -= ListBox_SelectionChanged;
       listBox.SelectionChanged += ListBox_SelectionChanged;
        SyncSelectedItems(listBox);
            }
        }

   private static void OnCollectionChanged(ListBox listBox, NotifyCollectionChangedEventArgs e)
        {
       // Re-sync the ListBox selection when the bound collection changes
            SyncSelectedItems(listBox);
        }

        private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
    if (sender is ListBox listBox)
            {
                var bindableSelectedItems = GetBindableSelectedItems(listBox);
         if (bindableSelectedItems == null) return;

                // Avoid re-entrancy: temporarily unsubscribe from collection changes
       if (bindableSelectedItems is INotifyCollectionChanged notifyCollection)
           {
        // Use a flag or just clear and repopulate
}

    bindableSelectedItems.Clear();
        foreach (var item in listBox.SelectedItems)
    {
       bindableSelectedItems.Add(item);
     }
            }
        }

        private static void SyncSelectedItems(ListBox listBox)
      {
       var bindableSelectedItems = GetBindableSelectedItems(listBox);
      if (bindableSelectedItems == null) return;

     // Temporarily unsubscribe to avoid re-entrancy
            listBox.SelectionChanged -= ListBox_SelectionChanged;

            listBox.SelectedItems.Clear();
        foreach (var item in bindableSelectedItems.Cast<object>().ToList())
            {
   // Only add if the item exists in the ListBox's ItemsSource
     if (listBox.Items.Contains(item))
                {
  listBox.SelectedItems.Add(item);
        }
      }

        // Re-subscribe
          listBox.SelectionChanged += ListBox_SelectionChanged;
        }
    }
}
