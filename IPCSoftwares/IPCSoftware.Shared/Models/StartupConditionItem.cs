using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models
{
    public class StartupConditionItem : ObservableObjectVM
    {
        public int TagId { get; set; }
        public string Description { get; set; }

        private bool _isMet;
        public bool IsMet
        {
            get => _isMet;
            set => SetProperty(ref _isMet, value);
        }
    }
}
