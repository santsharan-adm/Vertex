using CsvHelper;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IPCSoftware.AppLogger.Models;
using System.Configuration;

namespace IPCSoftware.App.ViewModels
{
    /// <summary>
    /// ViewModel responsible for loading folders, CSV files, and log records.
    /// Provides data binding for UI selection of folders, files, and the logs inside them.
    /// </summary>
    public class IPCLogsViewModel : INotifyPropertyChanged
    {
        // --------------------------------------------------------------------
        //  Public Collections
        // --------------------------------------------------------------------

        /// <summary>
        /// List of available log folders located in the root directory.
        /// </summary>
        public ObservableCollection<FolderInfo> Folders { get; } = new();

        /// <summary>
        /// List of CSV files inside the selected folder.
        /// </summary>
        public ObservableCollection<CsvFileInfo> CsvFiles { get; } = new();

        /// <summary>
        /// Loaded CSV log records from the selected file.
        /// </summary>
        public ObservableCollection<LogRecord> CsvData { get; } = new();



        // --------------------------------------------------------------------
        //  Backing fields
        // --------------------------------------------------------------------

        private CsvFileInfo _selectedFile;
        private FolderInfo _selectedFolder;
        private readonly string _rootFolder;   // Root folder for logs


        // --------------------------------------------------------------------
        //  Constructor
        // --------------------------------------------------------------------

        /// <summary>
        /// Initializes the ViewModel and loads available folders.
        /// </summary>
        public IPCLogsViewModel()
        {
            _rootFolder = ConfigurationManager.AppSettings["IPCLogsRootFolder"];
            LoadFolders();
        }

        // --------------------------------------------------------------------
        //  Properties
        // --------------------------------------------------------------------

        /// <summary>
        /// Currently selected CSV file. 
        /// When changed, automatically loads CSV contents.
        /// </summary>
        public CsvFileInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (_selectedFile != value)
                {
                    _selectedFile = value;
                    OnPropertyChanged();
                    LoadCsvData(_selectedFile?.FullPath);
                }
            }
        }

        /// <summary>
        /// Currently selected folder. 
        /// When changed, automatically loads list of contained CSV files.
        /// </summary>
        public FolderInfo SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                if (_selectedFolder != value)
                {
                    _selectedFolder = value;
                    OnPropertyChanged();
                    LoadCsvFiles(_selectedFolder?.FullPath);
                }
            }
        }


        // --------------------------------------------------------------------
        //  Private Methods - Data Loading
        // --------------------------------------------------------------------

        /// <summary>
        /// Loads folder names from the root directory.
        /// </summary>
        private void LoadFolders()
        {
            if (!Directory.Exists(_rootFolder)) return;

            Folders.Clear();

            var dirs = Directory.GetDirectories(_rootFolder);
            foreach (var dir in dirs)
            {
                Folders.Add(new FolderInfo
                {
                    FolderName = Path.GetFileName(dir),
                    FullPath = dir
                });
            }
        }

        /// <summary>
        /// Loads CSV files from the given folder.
        /// </summary>
        /// <param name="folder">Full folder path.</param>
        private void LoadCsvFiles(string folder)
        {
            CsvFiles.Clear();
            CsvData.Clear();

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return;

            var files = Directory.GetFiles(folder, "*.csv");

            foreach (var file in files)
            {
                CsvFiles.Add(new CsvFileInfo
                {
                    FileName = Path.GetFileName(file),
                    FullPath = file
                });
            }
        }

        /// <summary>
        /// Loads the contents of a CSV file and parses it into LogRecord objects.
        /// </summary>
        /// <param name="file">Full CSV file path.</param>
        private void LoadCsvData(string file)
        {
            CsvData.Clear();

            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                return;

            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<LogRecord>();

            foreach (var record in records)
                CsvData.Add(record);
        }


        // --------------------------------------------------------------------
        //  INotifyPropertyChanged Implementation
        // --------------------------------------------------------------------

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises a PropertyChanged event to notify UI of updates.
        /// </summary>
        /// <param name="propertyName">Property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}