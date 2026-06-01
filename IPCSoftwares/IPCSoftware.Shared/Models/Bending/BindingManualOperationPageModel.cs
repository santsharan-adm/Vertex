using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.Bending
{
    internal class ManualOperationPageModel
    {
        string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
            }
        }
        List<ManualOperationItemModel> _selectedButtons;
        public List<ManualOperationItemModel> SelectedButtons
        {
            get => _selectedButtons;
            set => SetProperty(ref _selectedButtons, value);
        }
    }
}
