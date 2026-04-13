using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    public class PLCTagModel : ModelBase
    {
        
        public uint Id { get; set; }

        public string Name { get; set; } = "Tag1";


        public uint PLCNo { get; set; } = 1;


        public uint ModbusAddress = 0;

       
        public byte DataLength = 1;
        public byte BitIndex = 1;
        public byte AlgoNo { get; set; } = 1;

        uint _offset = 0;
        public uint Offset { get; set; } = 0;

        public uint Span { get; set; } = 1;


        public object Value { get; set; }

        
        public bool IsBigEndian { get; set; }

        string _description;
        public string Description
        {
            get => _description;
            set
            {
                if (value != _description)
                {
                    _description = value;
                    RaisePropertyChanged(nameof(Description));
                }
            }
        }
        string _remark;
        public string Remark
        {
            get => _remark;
            set
            {
                if (value != _remark)
                {
                    _remark = value;
                    RaisePropertyChanged(nameof(Remark));
                }
            }
        }

        public override void Copy(ModelBase item)
        {
            base.Copy(item);
            if (item is PLCTagModel)
            {
                this.Name = ((PLCTagModel)item).Name;
                this.PLCNo = ((PLCTagModel)item).PLCNo;
                this.ModbusAddress = ((PLCTagModel)item).ModbusAddress;
                this.DataLength = ((PLCTagModel)item).DataLength;
                this.AlgoNo = ((PLCTagModel)item).AlgoNo;
                this.Offset = ((PLCTagModel)item).Offset;
                this.Span = ((PLCTagModel)item).Span;
                this.Description = ((PLCTagModel)item).Description;
                this.Remark = ((PLCTagModel)item).Remark;

            }
        }

        public void LoadFromStringArray(string[] data)
        {
            if (data.Length >= 9)
            {
                Id = uint.Parse(data[0]);
                Name = data[1];
                PLCNo = uint.Parse(data[2]);
                ModbusAddress = uint.Parse(data[3]);
                DataLength = byte.Parse(data[4]);
                AlgoNo = byte.Parse(data[5]);
                Offset = uint.Parse(data[6]);
                Span = uint.Parse(data[7]);
                Description = data[8];
                Remark = data[9];
            }
        }
    }
}
