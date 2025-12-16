using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class AlarmConfigurationModel
    {
        public int Id { get; set; }
        public int AlarmNo { get; set; }
        public string AlarmName { get; set; }
        public int TagNo { get; set; }
        public string Name { get; set; }
        public string AlarmBit { get; set; }
        public string AlarmText { get; set; }
        public string Severity { get; set; }
        public DateTime? AlarmTime { get; set; }
        public DateTime? AlarmResetTime { get; set; }
        public DateTime? AlarmAckTime { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }

        public AlarmConfigurationModel()
        {
            // Default values
            Severity = "Error";
        }

        public AlarmConfigurationModel Clone()
        {
            return new AlarmConfigurationModel
            {
                Id = this.Id,
                AlarmNo = this.AlarmNo,
                AlarmName = this.AlarmName,
                TagNo = this.TagNo,
                Name = this.Name,
                AlarmBit = this.AlarmBit,
                AlarmText = this.AlarmText,
                Severity = this.Severity,
                AlarmTime = this.AlarmTime,
                AlarmResetTime = this.AlarmResetTime,
                AlarmAckTime = this.AlarmAckTime,
                Description = this.Description,
                Remark = this.Remark
            };
        }
    }
}
