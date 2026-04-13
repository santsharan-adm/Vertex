using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    public class PLCData : ModelBase
    {
        public PLCData(uint unitNo,uint address, ushort value, uint algoNo)
        {
            this.UnitNo = unitNo;
            this.Address = address;
            this.Value = value;
            _algoNo = algoNo;
        }
        uint _unitNo;
        public uint UnitNo
        {
            get => _unitNo;
            set
            {
                if (value != _unitNo)
                {
                    _unitNo = value;
                    RaisePropertyChanged(nameof(UnitNo));
                }
            }
        }
        uint _address;
        public uint Address
        {
            get => _address;
            set
            {
                if (value != _address)
                {
                    _address = value;
                    RaisePropertyChanged(nameof(Address));
                }
            }
        }

        uint _algoNo;
        public uint AlgoNo { get => _algoNo; 
            set
            {
                if (value != _algoNo)
                {
                    _algoNo = value;
                    RaisePropertyChanged(nameof(AlgoNo));
                }
            }
        }

        object _value;
        public object Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    _value = value;
                    RaisePropertyChanged(nameof(Value));
                }
            }
        }
    }
}
