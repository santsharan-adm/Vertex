using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace IPCSoftware.App.ViewModels
{
    public class DashboardDetailViewModel : BaseViewModel
    {
        private string _cardTitle;

        public string CardTitle
        {
            get => _cardTitle;
            set => SetProperty(ref _cardTitle, value);
        }

        private string _windowTitle;

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public ObservableCollection<MetricDetailItem> DetailRows { get; } = new();

        public RelayCommand CloseCommand { get; }

        // We need a reference to the window to close it
        private Window _window;

        public DashboardDetailViewModel(Window window, string title, List<MetricDetailItem> data)
        {
            try
            {
                _window = window;
                WindowTitle = title;

                foreach (var item in data)
                {
                    DetailRows.Add(item);
                }

                CloseCommand = new RelayCommand(() => _window.Close());
            }
            catch (Exception)
            {
                
            }
        }
    }
}
