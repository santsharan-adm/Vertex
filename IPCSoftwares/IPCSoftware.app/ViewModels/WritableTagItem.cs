using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.App.ViewModels
{
    public class WritableTagItem : BaseViewModel
    {
        public PLCTagConfigurationModel Model { get; }

        public string DataTypeDisplay => GetDataTypeName(Model.DataType);

        public WritableTagItem(PLCTagConfigurationModel model)
        {
            Model = model;
        }
        private object _displayValue;
        public object DisplayValue
        {
            get => _displayValue;
            set => SetProperty(ref _displayValue, value);
        }


        private object _inputValue;
        public object InputValue
        {
            get => _inputValue;
            set => SetProperty(ref _inputValue, value);

        }


        private string GetDataTypeName(int typeId)
        {
            return typeId switch
            {
                1 => "Int16",
                2 => "Int32",
                3 => "Boolean",
                4 => "Float",
                5 => "String",
                6 => "UInt16",
                7 => "UInt32",
                _ => "Unknown"
            };
        }
    }

}
