using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
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
        private PLCTagConfigurationModel _selectedTag;

        public ObservableCollection<PLCTagConfigurationModel> Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
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
                foreach (var tag in tags)
                {
                    Tags.Add(tag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, LogType.Diagnostics);
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
