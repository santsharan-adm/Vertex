using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class PLCTagConfigurationModel
    {
        public int Id { get; set; }
        public int TagNo { get; set; }
        public string Name { get; set; }
        public int PLCNo { get; set; }
        public int ModbusAddress { get; set; }
        public int Length { get; set; }
        public int AlgNo { get; set; }
        public int DataType { get; set; }   // NEW
        public int BitNo { get; set; }      // NEW

        public int Offset { get; set; }
        public int Span { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }

        public bool CanWrite { get; set; }

        public PLCTagConfigurationModel()
        {
            Length = 1;
            AlgNo = 0;      // default Raw (per your rule)
            DataType = 1;   // default Int16
            BitNo = 0;      // safe default

            Offset = 0;
            Span = 100;
            CanWrite = false; 
        }

        public PLCTagConfigurationModel Clone()
        {
            return new PLCTagConfigurationModel
            {
                Id = this.Id,
                TagNo = this.TagNo,
                Name = this.Name,
                PLCNo = this.PLCNo,
                ModbusAddress = this.ModbusAddress,
                Length = this.Length,
                AlgNo = this.AlgNo,

                DataType = this.DataType,
                BitNo = this.BitNo,

                Offset = this.Offset,
                Span = this.Span,
                Description = this.Description,
                Remark = this.Remark,
                CanWrite = this.CanWrite
            };
        }
    }

}
