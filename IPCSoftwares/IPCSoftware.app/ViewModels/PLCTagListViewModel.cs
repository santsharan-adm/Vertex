using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IPCSoftware.App.ViewModels
{
    public class PLCTagListViewModel : BaseViewModel
    {
        private readonly IPLCTagConfigurationService _tagService;
        private readonly INavigationService _nav;
        private ObservableCollection<PLCTagConfigurationModel> _tags;
        private ObservableCollection<PLCTagConfigurationModel> _filteredTags;
        private PLCTagConfigurationModel _selectedTag;

        public ObservableCollection<PLCTagConfigurationModel> Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        public ObservableCollection<PLCTagConfigurationModel> FilteredTags
        {
            get => _filteredTags;
            set => SetProperty(ref _filteredTags, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }


        public PLCTagConfigurationModel SelectedTag
        {
            get => _selectedTag;
            set => SetProperty(ref _selectedTag, value);
        }

        public ICommand AddTagCommand { get; }
        public ICommand EditTagCommand { get; }
        public ICommand DeleteTagCommand { get; }

        public PLCTagListViewModel(
            IPLCTagConfigurationService tagService, 
            INavigationService nav,
            IAppLogger logger) : base(logger)
        {
            _tagService = tagService;
            _nav = nav;
            Tags = new ObservableCollection<PLCTagConfigurationModel>();
            FilteredTags = new ObservableCollection<PLCTagConfigurationModel>();

            AddTagCommand = new RelayCommand(OnAddTag);
            EditTagCommand = new RelayCommand<PLCTagConfigurationModel>(OnEditTag);
            DeleteTagCommand = new RelayCommand<PLCTagConfigurationModel>(OnDeleteTag);

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var tags = await _tagService.GetAllTagsAsync();
                Tags.Clear();
                FilteredTags.Clear();
                foreach (var tag in tags)
                {
                    Tags.Add(tag);
                    FilteredTags.Add(tag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }

        private void ApplyFilter()
        {
            FilteredTags.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var user in Tags)
                {
                    FilteredTags.Add(user);
                }
            }   
            else
            {
                var filtered = Tags.Where(u =>
                    (u.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.ModbusAddress.ToString()?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.PLCNo.ToString()?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.TagNo.ToString()?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                );

                foreach (var user in filtered)
                {
                    FilteredTags.Add(user);
                }
            }
        }


        private void OnAddTag()
        {
            _nav.NavigateToPLCTagConfiguration(null, async () =>
            {
                await LoadDataAsync();
            });
        }

        private void OnEditTag(PLCTagConfigurationModel tag)
        {
            if (tag == null) return;

            _nav.NavigateToPLCTagConfiguration(tag, async () =>
            {
                await LoadDataAsync();
            });
        }

        private async void OnDeleteTag(PLCTagConfigurationModel tag)
        {
            try
            {
                if (tag == null) return;

                // TODO: Add confirmation dialog
                await _tagService.DeleteTagAsync(tag.Id);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
            }
        }
    }
}
