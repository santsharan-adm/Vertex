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
        public string ModbusAddress { get; set; }
        public int Length { get; set; }
        public int AlgNo { get; set; }  // NOW INT: 1=Linear, 2=FP, 3=String
        public int Offset { get; set; }
        public int Span { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }

        public PLCTagConfigurationModel()
        {
            Length = 1;
            AlgNo = 1;  // Default to Linear scale
            Offset = 0;
            Span = 100;
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
                Offset = this.Offset,
                Span = this.Span,
                Description = this.Description,
                Remark = this.Remark
            };
        }
    }
}
