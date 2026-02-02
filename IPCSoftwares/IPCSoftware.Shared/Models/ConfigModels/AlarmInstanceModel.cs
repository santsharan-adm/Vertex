using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class AlarmInstanceModel : INotifyPropertyChanged
    {
        public Guid InstanceId { get; set; }
        public int AlarmNo { get; set; }
        public string AlarmText { get; set; }
        public string Severity { get; set; }
        public DateTime AlarmTime { get; set; }
        public int SerialNo { get; set; } //added property for tracking and binding

        // REMOVED the auto-property: public DateTime? AlarmResetTime { get; set; } 

        private DateTime? _alarmAckTime;
        private string _acknowledgedByUser;
        private DateTime? _alarmResetTime; // Backing field

        public DateTime? AlarmAckTime
        {
            get => _alarmAckTime;
            set
            {
                if (_alarmAckTime != value)
                {
                    _alarmAckTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AcknowledgedByUser
        {
            get => _acknowledgedByUser;
            set
            {
                if (_acknowledgedByUser != value)
                {
                    _acknowledgedByUser = value;
                    OnPropertyChanged();
                }
            }
        }

        // KEEP ONLY THIS ONE for Reset Time
        public DateTime? AlarmResetTime
        {
            get => _alarmResetTime;
            set
            {
                if (_alarmResetTime != value)
                {
                    _alarmResetTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}