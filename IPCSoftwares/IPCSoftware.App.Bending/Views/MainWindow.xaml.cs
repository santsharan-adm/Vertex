using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IPCSoftware.App.Bending.Views;

namespace IPCSoftware.App.Bending
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Initialize menu items
            SidebarItems.Add("Bending Monitor 1");
            SidebarItems.Add("Bending Monitor 2");
            SidebarItems.Add("Bending Monitor 3");
            SidebarItems.Add("User Control 1");
            SidebarItems.Add("User Control 2");

            // Set default content
            var uc1 = new UserControl1();
            uc1.DataContext = this; // Pass MainWindow as DataContext
            MainContentArea.Content = uc1;
        }

        // ==============================
        // SIDEBAR PROPERTIES
        // ==============================
        private bool _isSidebarOpen;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set
            {
                _isSidebarOpen = value;
                OnPropertyChanged();
            }
        }

        private bool _isSidebarDocked;
        public bool IsSidebarDocked
        {
            get => _isSidebarDocked;
            set
            {
                _isSidebarDocked = value;
                OnPropertyChanged();
                // If we dock, we must ensure it's open
                if (value)
                    IsSidebarOpen = true;
                else
                    IsSidebarOpen = false;
            }
        }

        public ObservableCollection<string> SidebarItems { get; } = new ObservableCollection<string>();

        // ==============================
        // COMMANDS
        // ==============================
        private ICommand _toggleSidebarCommand;
        public ICommand ToggleSidebarCommand => _toggleSidebarCommand ??= new RelayCommand(() => IsSidebarOpen = !IsSidebarOpen);

        private ICommand _closeSidebarCommand;
        public ICommand CloseSidebarCommand => _closeSidebarCommand ??= new RelayCommand(() => IsSidebarOpen = false);

        private ICommand _sidebarItemClickCommand;
        public ICommand SidebarItemClickCommand => _sidebarItemClickCommand ??= new RelayCommand<string>(OnSidebarItemClick);

        // ==============================
        // MENU NAVIGATION
        // ==============================
        private void OnSidebarItemClick(string itemName)
        {
            // Close sidebar if not docked
            if (!IsSidebarDocked)
            {
                IsSidebarOpen = false;
            }

            // Navigate based on item name
            switch (itemName)
            {
                case "Bending Monitor 1":
                    var bending1 = new Bending1MonitorView();
                    bending1.DataContext = this; // Pass MainWindow as DataContext
                    MainContentArea.Content = bending1;
                    break;
                case "Bending Monitor 2":
                    var bending2 = new Bending2MonitorView();
                    bending2.DataContext = this; // Pass MainWindow as DataContext
                    MainContentArea.Content = bending2;
                    break;
                case "Bending Monitor 3":
                    var bending3 = new Bending3MonitorView();
                    bending3.DataContext = this; // Pass MainWindow as DataContext
                    MainContentArea.Content = bending3;
                    break;
                case "User Control 1":
                    var uc1 = new UserControl1();
                    uc1.DataContext = this; // Pass MainWindow as DataContext
                    MainContentArea.Content = uc1;
                    break;
                case "User Control 2":
                    var uc2 = new UserControl2();
                    uc2.DataContext = this; // Pass MainWindow as DataContext
                    MainContentArea.Content = uc2;
                    break;
            }
        }

        // ==============================
        // INotifyPropertyChanged Implementation
        // ==============================
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ==============================
    // RELAY COMMAND IMPLEMENTATION
    // ==============================
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter) => _execute((T)parameter);
    }
}







//Show only single screen


//using System.Windows;

//namespace IPCSoftware.App.Bending
//{
//    public partial class MainWindow : Window
//    {
//        public MainWindow()
//        {
//            InitializeComponent();
//        }
//    }
//}