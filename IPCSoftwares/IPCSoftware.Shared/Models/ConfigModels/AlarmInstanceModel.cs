using System;
using System.Collections.Generic;
using System.ComponentModel; // ⬅️ REQUIRED for INotifyPropertyChanged
using System.Linq;
using System.Runtime.CompilerServices; // ⬅️ RECOMMENDED for [CallerMemberName]
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    // ➡️ 1. INHERIT OR IMPLEMENT INotifyPropertyChanged
    public class AlarmInstanceModel : INotifyPropertyChanged
    {
        // ... (Existing properties - InstanceId, AlarmNo, AlarmText, Severity, AlarmTime, AlarmResetTime - can stay as auto-properties if they don't change after creation) ...
        public Guid InstanceId { get; set; }
        public int AlarmNo { get; set; }
        public string AlarmText { get; set; }
        public string Severity { get; set; }
        public DateTime AlarmTime { get; set; }
        public DateTime? AlarmResetTime { get; set; }


        // --- Runtime State Fields (Must be Observable) ---

        // ➡️ 2. BACKING FIELDS for the observable properties
        private DateTime? _alarmAckTime;
        private string _acknowledgedByUser;

        // ➡️ 3. Observable Setter for AlarmAckTime
        public DateTime? AlarmAckTime
        {
            get => _alarmAckTime;
            set
            {
                if (_alarmAckTime != value)
                {
                    _alarmAckTime = value;
                    OnPropertyChanged(); // ⬅️ CRITICAL FIX
                }
            }
        }

        // ➡️ 4. Observable Setter for AcknowledgedByUser
        public string AcknowledgedByUser
        {
            get => _acknowledgedByUser;
            set
            {
                if (_acknowledgedByUser != value)
                {
                    _acknowledgedByUser = value;
                    OnPropertyChanged(); // ⬅️ CRITICAL FIX
                }
            }
        }

        // ➡️ 5. INotifyPropertyChanged Implementation (If not using a BaseModel)
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}