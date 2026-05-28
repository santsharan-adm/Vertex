using System;
using IPCSoftware.Shared;

namespace IPCSoftware.Shared.Models.Bending
{
    public class BatchModel : ObservableObjectVM
    {
        // Identity
        private string _batchNumber = string.Empty;
        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        private int _stage = 1;
        public int Stage
        {
            get => _stage;
            set => SetProperty(ref _stage, value);
        }

        // QR Codes (4 parts)
        private string _qrCode1 = string.Empty;
        public string QrCode1
        {
            get => _qrCode1;
            set => SetProperty(ref _qrCode1, value);
        }

        private string _qrCode2 = string.Empty;
        public string QrCode2
        {
            get => _qrCode2;
            set => SetProperty(ref _qrCode2, value);
        }

        private string _qrCode3 = string.Empty;
        public string QrCode3
        {
            get => _qrCode3;
            set => SetProperty(ref _qrCode3, value);
        }

        private string _qrCode4 = string.Empty;
        public string QrCode4
        {
            get => _qrCode4;
            set => SetProperty(ref _qrCode4, value);
        }

        // Bending-1 data (Stage 3): 4 Temperatures, 4 Loads
        public float[] Bending1Temperatures { get; set; } = new float[4];
        public float[] Bending1Loads { get; set; } = new float[4];

        // Bending-2 data (Stage 3): 4 Temperatures, 4 Loads
        public float[] Bending2Temperatures { get; set; } = new float[4];
        public float[] Bending2Loads { get; set; } = new float[4];

        // Bending-3 data (Stage 4): 4 Temperatures, 4 Loads, 4 X, 4 Y, 4 Z, 4 W
        public float[] Bending3Temperatures { get; set; } = new float[4];
        public float[] Bending3Loads { get; set; } = new float[4];
        public float[] Bending3X { get; set; } = new float[4];
        public float[] Bending3Y { get; set; } = new float[4];
        public float[] Bending3Z { get; set; } = new float[4];
        public float[] Bending3W { get; set; } = new float[4];

        // Tearing data (Stage 6): 4 Temperatures
        public float[] TearingTemperatures { get; set; } = new float[4];

        // Flipping data (Stage 7): 4 Forces
        public float[] FlippingForces { get; set; } = new float[4];

        // Inspection results (Stage 9): 4 booleans
        public bool[] InspectionResults { get; set; } = new bool[4];

        // Metadata
        private DateTime _createdAt;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        private DateTime? _arrivedAtStage9;
        public DateTime? ArrivedAtStage9
        {
            get => _arrivedAtStage9;
            set => SetProperty(ref _arrivedAtStage9, value);
        }

        private bool _inspectionComplete;
        public bool InspectionComplete
        {
            get => _inspectionComplete;
            set => SetProperty(ref _inspectionComplete, value);
        }

        private bool _dataLogged;
        public bool DataLogged
        {
            get => _dataLogged;
            set => SetProperty(ref _dataLogged, value);
        }
    }
}
