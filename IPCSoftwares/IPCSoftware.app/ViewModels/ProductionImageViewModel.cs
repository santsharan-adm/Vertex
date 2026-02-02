using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Extensions.Options;

namespace IPCSoftware.App.ViewModels
{
    public class ProductionImageViewModel : BaseViewModel
    {
        private readonly ILogConfigurationService _logConfig;
        private string _rootPath;

        // Collections
        private List<ProductionFolderModel> _allFolders = new();
        public ObservableCollection<ProductionFolderModel> Folders { get; } = new();
        public ObservableCollection<ProductionImageModel> Images { get; } = new();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterFolders(); // Filter immediately when typing
                }
            }

        }
        // State
        private ProductionFolderModel _selectedFolder;
        public ProductionFolderModel SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                if (SetProperty(ref _selectedFolder, value) && value != null)
                {
                    _ = LoadImagesAsync(value.FullPath);
                }
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand SaveImageCommand { get; }

        public ProductionImageViewModel(
            ILogConfigurationService logConfig,
            IOptions <ExternalSettings> extSetting,
            IAppLogger logger) : base(logger)
        {
            var machineFolder = extSetting.Value.AOIMachineCode;
            var prodFolder = "Production Images";
            _logConfig = logConfig;
            _= InitializeAsync(machineFolder, prodFolder);
            RefreshCommand = new RelayCommand(async () => await InitializeAsync(machineFolder, prodFolder));
            SaveImageCommand = new RelayCommand<ProductionImageModel>(OnSaveImage);
        }

        public async Task InitializeAsync(string machineFolder, string prodFlder)
        {
            try
            {
                // 1. Get Root Path from Config
                var logConfig = await _logConfig.GetByLogTypeAsync(LogType.Production);
                var logFolderPath = logConfig.ProductionImagePath;
                
                _rootPath = Path.Combine(logFolderPath, machineFolder, prodFlder);

                if (string.IsNullOrWhiteSpace(_rootPath) || !Directory.Exists(_rootPath))
                {
                    StatusMessage = "Production Image Path is not configured or does not exist.";
                    Folders.Clear();
                    Images.Clear();
                    return;
                }

                StatusMessage = $"Root: {_rootPath}";
                await LoadFoldersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void FilterFolders()
        {
            if (_allFolders == null) return;

            Folders.Clear();

            // Case-insensitive search
            var query = string.IsNullOrWhiteSpace(SearchText)
                ? _allFolders
                : _allFolders.Where(f => f.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var folder in query)
            {
                Folders.Add(folder);
            }
        }

        private void OnSaveImage(ProductionImageModel img)
        {
            if (img == null || !File.Exists(img.FullPath)) return;

            try
            {
                // Open Save Dialog
                var saveDialog = new SaveFileDialog
                {
                    FileName = img.FileName, // Default file name
                    DefaultExt = Path.GetExtension(img.FullPath), // Default file extension
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*" // Filter
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Copy the file from Source (Network/Folder) to Destination (User selected path)
                    File.Copy(img.FullPath, saveDialog.FileName, true);

                    // Optional: Update Status Bar to show success
                    StatusMessage = $"Saved: {Path.GetFileName(saveDialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Save Image Error: {ex.Message}", LogType.Diagnostics);
                StatusMessage = "Error saving image.";
            }
        }

        private async Task LoadFoldersAsync()
        {
            try
            {
                // Clear current view
                Folders.Clear();
                _allFolders.Clear();

                await Task.Run(() =>
                {
                    // 1. Get All Folders
                    var dirs = new DirectoryInfo(_rootPath).GetDirectories()
                                .OrderByDescending(d => d.CreationTime)
                                .Select(d => new ProductionFolderModel
                                {
                                    Name = d.Name,
                                    FullPath = d.FullName,
                                    CreatedDate = d.CreationTime
                                })
                                .ToList();

                    // 2. Save to Master List
                    _allFolders = dirs;

                    // 3. Update UI
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var d in dirs) Folders.Add(d);
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Folder Scan Error: {ex.Message}", LogType.Diagnostics);
            }
        }

        private async Task LoadImagesAsync(string folderPath)
        {
            try
            {
                Images.Clear();
                    
                await Task.Run(() =>
                {
                    var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                                {
                                                    ".jpg", ".jpeg", ".png", ".bmp"
                                                };

                    var dirInfo = new DirectoryInfo(folderPath);
                    if (!dirInfo.Exists) return;

                    var files = dirInfo.EnumerateFiles();

                    // Local Helper Function to grab the integer prefix
                    // Example: "154_Result.jpg" -> returns 154
                    // Example: "Error.jpg" -> returns int.MaxValue (puts it at the bottom)
                    int GetPrefix(string fileName)
                    {
                        string numberString = new string(fileName.TakeWhile(char.IsDigit).ToArray());
                        return int.TryParse(numberString, out int result) ? result : int.MaxValue;
                    }

                    var imageFiles = files
                        .Where(f => allowedExtensions.Contains(f.Extension))
                        .OrderBy(f => GetPrefix(f.Name)) // <--- SORT BY INTEGER PREFIX
                        .Select(f => new ProductionImageModel
                        {
                            FileName = f.Name,
                            FullPath = f.FullName
                        })
                        .ToList();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var img in imageFiles)
                        {
                            Images.Add(img);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Image Load Error: {ex.Message}", LogType.Diagnostics);
            }
        }
    }


}