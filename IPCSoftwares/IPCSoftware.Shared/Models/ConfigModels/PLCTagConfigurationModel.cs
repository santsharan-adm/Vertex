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
       // public string DMAddress { get; set; } // NEW: DM Address (e.g., DM100)
        public int Length { get; set; }
        public int AlgNo { get; set; }
        public int DataType { get; set; }
        public int BitNo { get; set; }

        public int Offset { get; set; }
        public int Span { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }

        public bool CanWrite { get; set; }
        public string IOType { get; set; } // NEW: Input/Output

        public PLCTagConfigurationModel()
        {
            Length = 1;
            AlgNo = 0;
            DataType = 1;
            BitNo = 0;

            Offset = 0;
            Span = 100;
            CanWrite = false;
            IOType = "None"; // Default
           // DMAddress = string.Empty;
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
              //  DMAddress = this.DMAddress, // Clone new field
                Length = this.Length,
                AlgNo = this.AlgNo,

                DataType = this.DataType,
                BitNo = this.BitNo,

                Offset = this.Offset,
                Span = this.Span,
                Description = this.Description,
                Remark = this.Remark,
                CanWrite = this.CanWrite,
                IOType = this.IOType // Clone new field
            };
        }
    }
}