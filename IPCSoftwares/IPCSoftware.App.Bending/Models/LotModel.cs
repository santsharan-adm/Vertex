using System;
using System.Collections.ObjectModel;

namespace IPCSoftware.App.Bending.Models
{
    /// <summary>
    /// Represents a manufacturing lot with QR codes and parameters
    /// </summary>
    public class LotModel
    {
        public string Id { get; set; }
        public ObservableCollection<string> QRCodes { get; set; }
        public double[] Temperature { get; set; }
        public double[] Pressure { get; set; }
        public DateTime CreatedTime { get; set; }
        public LotStatus Status { get; set; }

        public LotModel(string id)
        {
            Id = id;
            QRCodes = new ObservableCollection<string>();
            Temperature = new double[4];
            Pressure = new double[4];
            CreatedTime = DateTime.Now;
            Status = LotStatus.Pending;
        }
    }

    /// <summary>
    /// Lot processing status
    /// </summary>
    public enum LotStatus
    {
        Pending,
        InProgress,
        Completed,
        Rejected
    }
}
