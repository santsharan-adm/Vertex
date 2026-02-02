using IPCSoftware.App.NavServices;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IPCSoftware.App.Helpers
{
    internal class BlockTabChangeBehavior
    {
        public static readonly DependencyProperty BlockWhenProperty =
        DependencyProperty.RegisterAttached(
            "BlockWhen",
            typeof(bool),
            typeof(BlockTabChangeBehavior),
            new PropertyMetadata(false));

        public static bool GetBlockWhen(DependencyObject obj)
            => (bool)obj.GetValue(BlockWhenProperty);

        public static void SetBlockWhen(DependencyObject obj, bool value)
            => obj.SetValue(BlockWhenProperty, value);

        private static readonly DependencyProperty LastValidIndexProperty =
            DependencyProperty.RegisterAttached(
                "LastValidIndex",
                typeof(int),
                typeof(BlockTabChangeBehavior),
                new PropertyMetadata(0));

        private static int GetLastValidIndex(DependencyObject obj)
            => (int)obj.GetValue(LastValidIndexProperty);

        private static void SetLastValidIndex(DependencyObject obj, int value)
            => obj.SetValue(LastValidIndexProperty, value);

        static BlockTabChangeBehavior()
        {
            EventManager.RegisterClassHandler(
                typeof(TabControl),
                TabControl.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnSelectionChanged));
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl tab || e.Source != tab)
                return;

            int newIndex = tab.SelectedIndex;
            int lastIndex = GetLastValidIndex(tab);

            // Ignore re-selecting same tab
            if (newIndex == lastIndex)
                return;

            if (GetBlockWhen(tab))
            {
                // 🔔 SHOW MESSAGE ONLY WHEN USER TRIES TO SWITCH
                DialogService dialog = new DialogService();
                dialog.ShowWarning(
                    "⚠️ Unsaved changes detected.\n\n You must SAVE your changes before switching tabs.");

                // ⛔ revert tab
                tab.Dispatcher.BeginInvoke(() =>
                {
                    tab.SelectedIndex = lastIndex;
                });
            }
            else
            {
                // ✅ allow change
                SetLastValidIndex(tab, newIndex);
            }
        }
    }
}
