using IPCSoftware.App.Helpers;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace IPCSoftware.App.ViewModels
{
    public class StartupConditionViewModel : BaseViewModel, IDisposable
    {
        private readonly CoreClient _coreClient;
        private readonly SafePoller _timer;
        private readonly IPLCTagConfigurationService _tagService;
        private readonly string _configPath;
        private readonly string _dataFolder;

        // Collection bound to UI
        public ObservableCollection<StartupConditionItem> Conditions { get; } = new();

        // Overall Status (Green bar at bottom)
        private bool _allConditionsMet;
        public bool AllConditionsMet
        {
            get => _allConditionsMet;
            set => SetProperty(ref _allConditionsMet, value);
        }

        public StartupConditionViewModel(IOptions<ConfigSettings> configSettings,
            IPLCTagConfigurationService tagService,
            CoreClient coreClient, IAppLogger logger) : base(logger)
        {
            var config = configSettings.Value;
            _tagService = tagService;
            string dataFolderPath = config.DataFolder;

            _coreClient = coreClient;
            _dataFolder = dataFolderPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _configPath = Path.Combine(_dataFolder, "StartupConditions.json");
            _ = InitializeAsync();
         

            _timer = new SafePoller(TimeSpan.FromMilliseconds(100), OnTimerTick);
            _timer.Start();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // 1. Load the list of IDs from JSON
                if (!File.Exists(_configPath))
                {
                    _logger.LogError($"Startup Config not found: {_configPath}", LogType.Diagnostics);
                    return;
                }

                string json = await File.ReadAllTextAsync(_configPath);
                var configList = JsonConvert.DeserializeObject<List<StartupConditionConfig>>(json);

                if (configList == null || configList.Count == 0) return;

                // 2. Fetch ALL Tag Details (Descriptions) from Service
                var allTags = await _tagService.GetAllTagsAsync();

                // 3. Match and Populate
                // We do this on the UI thread to populate the ObservableCollection safely
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Conditions.Clear();
                    foreach (var item in configList)
                    {
                        // Find the tag definition
                        var tagDef = allTags.FirstOrDefault(t => t.Id == item.TagId); // Or t.TagNo depending on your model

                        if (tagDef != null)
                        {
                            Conditions.Add(new StartupConditionItem
                            {
                                TagId = item.TagId,
                                // Use Description if available, fallback to Name
                                Description = !string.IsNullOrWhiteSpace(tagDef.Description) ? tagDef.Description : tagDef.Name,
                                IsMet = false
                            });
                        }
                        else
                        {
                            // Fallback if tag ID in JSON doesn't exist in DB/Service
                            Conditions.Add(new StartupConditionItem
                            {
                                TagId = item.TagId,
                                Description = $"Unknown Tag ({item.TagId})",
                                IsMet = false
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing startup conditions: {ex.Message}", LogType.Diagnostics);
            }
        }


        private async Task OnTimerTick()
        {
            try
            {
                // Poll ID 5 (IO Data)
                var data = await _coreClient.GetIoValuesAsync(5);

                if (data != null)
                {
                    bool allMet = true;

                    // Update UI on Dispatcher (Collection changes usually require this, 
                    // though simple PropertyChanged often works cross-thread in newer WPF, safer to be sure)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var cond in Conditions)
                        {
                            if (data.TryGetValue(cond.TagId, out object val))
                            {
                                bool state = false;
                                if (val is bool b) state = b;
                                else if (val is int i) state = i > 0;

                                cond.IsMet = state;

                                if (!state) allMet = false;
                            }
                            else
                            {
                                // Tag not found in packet -> Treat as False
                                cond.IsMet = false;
                                allMet = false;
                            }
                        }

                        AllConditionsMet = allMet;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}