# Bending Machine HMI - XAML Architecture Guide

## Overview
This document outlines the XAML component structure and ViewModel architecture for the Bending Machine HMI. Follow this structure when converting from the HTML prototype to WPF/XAML components.

---

## Component Hierarchy

```
BendingDashboard.xaml (Main View)
│
├── HeaderControl.xaml
│   ├── Machine Status (Binding)
│   └── Quick Metrics (Binding)
│
├── LotTrackingControl.xaml
│   ├── CurrentLotCard
│   │   ├── UID Display (Binding)
│   │   ├── QR Code List (ItemsSource)
│   │   └── Stage Status (Binding)
│   │
│   └── QueueCard
│       └── Queue List (ItemsSource)
│
├── TurnTableVisualizationControl.xaml
│   ├── TurnTable1Card
│   │   ├── Animated Rotation (Storyboard)
│   │   ├── Position Indicator (Binding)
│   │   └── Current Position Label (Binding)
│   │
│   └── TurnTable2Card
│       └── (Same as TurnTable1)
│
├── StageContainer.xaml
│   ├── StageColumn (Repeat 3x for UAT1, UAT2, UAT3)
│   │   ├── StageHeader (Binding: Color, Title, Status)
│   │   │
│   │   ├── CurrentStepPanel.xaml
│   │   │   ├── Step Number (Binding)
│   │   │   └── Activity Description (Binding)
│   │   │
│   │   ├── ParametersPanel.xaml
│   │   │   ├── ParameterBox (Repeat 4x)
│   │   │   │   ├── Temperature/Pressure Label
│   │   │   │   ├── Value (Binding)
│   │   │   │   ├── Status Indicator (Binding: OK/NG Color)
│   │   │   │   └── Spec Range (Static/Binding)
│   │   │
│   │   ├── MeasurementsPanel.xaml
│   │   │   └── MeasurementItem (Repeat 4x: X, Y, Z, Ø)
│   │   │       ├── Label
│   │   │       ├── Value (Binding)
│   │   │       └── Spec Range (Binding)
│   │   │
│   │   ├── ProgressPanel.xaml
│   │   │   ├── Percentage (Binding)
│   │   │   └── ProgressBar (Binding)
│   │   │
│   │   └── ResultPanel.xaml
│   │       └── Result Badge (Binding: Color, Text)
│   │
│   └── RepeatLayout 3 times for each UAT stage
│
└── SystemMetricsBar.xaml
    └── MetricItem (Repeat 6x)
        ├── Label
        └── Value (Binding)
```

---

## ViewModel Structure

### Main ViewModel: `BendingDashboardViewModel.cs`

```csharp
public class BendingDashboardViewModel : ViewModelBase
{
    // ===================== HEADER SECTION =====================
    public string MachineStatus { get; set; } = "RUNNING";
    public double CurrentCycleTime { get; set; } = 12.4;
    public int PartsProcessedToday { get; set; } = 2847;

    // ===================== LOT TRACKING SECTION =====================
    public CurrentLotViewModel CurrentLot { get; set; }
    public ObservableCollection<LotViewModel> QueuedLots { get; set; }

    // ===================== TURNTABLE SECTION =====================
    public TurnTableViewModel TurnTable1 { get; set; }
    public TurnTableViewModel TurnTable2 { get; set; }

    // ===================== STAGE VIEWMODELS =====================
    public StageViewModel Stage1_UAT1 { get; set; }
    public StageViewModel Stage2_UAT2 { get; set; }
    public StageViewModel Stage3_UAT3 { get; set; }

    // ===================== SYSTEM METRICS =====================
    public int CycleCount { get; set; } = 245;
    public int TotalOKParts { get; set; } = 2835;
    public int TotalNGParts { get; set; } = 12;
    public string LastNGPartQR { get; set; } = "ABCD12345A002";
    public double UptimePercentage { get; set; } = 99.5;
    public string RunMode { get; set; } = "AUTOMATIC";

    // ===================== INITIALIZATION =====================
    public BendingDashboardViewModel()
    {
        InitializeLots();
        InitializeTurnTables();
        InitializeStages();
        StartModbusPolling();
    }
}
```

---

### Sub-ViewModels

#### 1. **CurrentLotViewModel.cs**

```csharp
public class CurrentLotViewModel : ViewModelBase
{
    public string LotUID { get; set; } = "LOT-001-2025";
    public ObservableCollection<string> CapturedQRCodes { get; set; }
    public string Stage1Status { get; set; } = "✓ Stage 1: QR Scan Complete";

    public CurrentLotViewModel()
    {
        CapturedQRCodes = new ObservableCollection<string>
        {
            "ABCD12345A001",
            "ABCD12345A002",
            "ABCD12345A003",
            "ABCD12345A004"
        };
    }
}
```

#### 2. **LotViewModel.cs** (For Queue)

```csharp
public class LotViewModel : ViewModelBase
{
    public string LotName { get; set; }
    public int PartCount { get; set; }
    public string Status { get; set; }
}
```

#### 3. **TurnTableViewModel.cs**

```csharp
public class TurnTableViewModel : ViewModelBase
{
    public string TableName { get; set; }

    private double _rotationAngle = 0;
    public double RotationAngle
    {
        get => _rotationAngle;
        set { _rotationAngle = value; OnPropertyChanged(); }
    }

    private int _currentPosition = 0;
    public int CurrentPosition
    {
        get => _currentPosition;
        set { _currentPosition = value; OnPropertyChanged(); }
    }

    public string PositionLabel => $"Pos {CurrentPosition}";

    public TurnTableViewModel(string name)
    {
        TableName = name;
    }

    public void Rotate()
    {
        var animation = new DoubleAnimation
        {
            From = RotationAngle,
            To = RotationAngle + 360,
            Duration = new Duration(TimeSpan.FromSeconds(8)),
            RepeatBehavior = RepeatBehavior.Forever
        };
        // Apply animation
    }
}
```

#### 4. **StageViewModel.cs** (Most Important)

```csharp
public class StageViewModel : ViewModelBase
{
    // ===================== STAGE IDENTIFICATION =====================
    public string StageName { get; set; } // "Bending UAT #1", etc.
    public string StageIndicator { get; set; } // "STAGE 1", etc.

    // ===================== STATUS & COLORS =====================
    private StageStatus _status = StageStatus.NotStarted;
    public StageStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); UpdateHeaderColor(); }
    }

    public string HeaderBackground => Status switch
    {
        StageStatus.Active => "Green",
        StageStatus.InProgress => "Blue",
        StageStatus.Completed => "Gray",
        StageStatus.Error => "Red",
        _ => "Gray"
    };

    // ===================== CURRENT STEP =====================
    public int CurrentStepNumber { get; set; } = 13;
    public int TotalSteps { get; set; } = 16;
    public string CurrentActivity { get; set; } = "Punch Forward";

    public string StepDisplay => $"Step {CurrentStepNumber} / {TotalSteps}";

    // ===================== PARAMETERS (Temperature & Pressure) =====================
    public ObservableCollection<ParameterViewModel> Parameters { get; set; }

    // Example structure for Stage 1:
    // - ParameterViewModel T1 (80.2°C, OK, Spec: 50-60)
    // - ParameterViewModel T2 (80.1°C, OK, Spec: 50-60)
    // - ParameterViewModel T3 (80.3°C, OK, Spec: 50-60)
    // - ParameterViewModel T4 (80.0°C, OK, Spec: 50-60)
    // - ParameterViewModel P1 (7.0N, OK, Spec: 5-10)
    // - ParameterViewModel P2 (7.1N, OK, Spec: 5-10)
    // - ParameterViewModel P3 (6.8N, OK, Spec: 5-10)
    // - ParameterViewModel P4 (6.9N, OK, Spec: 5-10)

    // ===================== MEASUREMENTS =====================
    public ObservableCollection<MeasurementViewModel> Measurements { get; set; }

    // Post-bend measurements:
    // - X: 1.6 (Spec: >1 <2)
    // - Y: 3.4 (Spec: >3 <4)
    // - Z: 5.4 (Spec: >5 <6)
    // - Ø: 7.5 (Spec: >7 <8)

    // ===================== PROGRESS =====================
    private double _progressPercentage = 0;
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set { _progressPercentage = value; OnPropertyChanged(); }
    }

    // ===================== RESULT =====================
    private ProcessResult _result = ProcessResult.Pending;
    public ProcessResult Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(); UpdateResultColor(); }
    }

    public string ResultDisplay => Result switch
    {
        ProcessResult.OK => "✓ OK",
        ProcessResult.NG => "✗ NG",
        ProcessResult.Pending => "PENDING",
        ProcessResult.NotStarted => "NOT STARTED",
        _ => "UNKNOWN"
    };

    public string ResultColor => Result switch
    {
        ProcessResult.OK => "#10B981",
        ProcessResult.NG => "#EF4444",
        ProcessResult.Pending => "#94A3B8",
        ProcessResult.NotStarted => "#94A3B8",
        _ => "#64748B"
    };

    // ===================== INITIALIZATION =====================
    public StageViewModel(string stageName, string indicator)
    {
        StageName = stageName;
        StageIndicator = indicator;
        Parameters = new ObservableCollection<ParameterViewModel>();
        Measurements = new ObservableCollection<MeasurementViewModel>();
        InitializeParameters();
        InitializeMeasurements();
    }

    private void InitializeParameters()
    {
        // For Stage 1: Add T1, T2, T3, T4, P1, P2, P3, P4
        // For Stage 2: Add T5, T6, T7, T8, P5, P6, P7, P8
        // For Stage 3: Add T9, T10, T11, T12, P9, P10, P11, P12
    }

    private void InitializeMeasurements()
    {
        // Add X, Y, Z, Ø measurements with specs
    }
}

public enum StageStatus { NotStarted, Active, InProgress, Completed, Error }
public enum ProcessResult { NotStarted, Pending, OK, NG }
```

#### 5. **ParameterViewModel.cs**

```csharp
public class ParameterViewModel : ViewModelBase
{
    public string ParameterName { get; set; } // "Temperature", "Pressure P1", etc.
    public string ParameterID { get; set; } // "T1", "P1", "T5", etc.

    private double _currentValue;
    public double CurrentValue
    {
        get => _currentValue;
        set
        {
            _currentValue = value;
            OnPropertyChanged();
            ValidateAgainstSpec();
        }
    }

    public double MinSpec { get; set; }
    public double MaxSpec { get; set; }
    public string SpecDisplay => $"Min: {MinSpec} | Max: {MaxSpec}";

    private bool _isOK = true;
    public bool IsOK
    {
        get => _isOK;
        set { _isOK = value; OnPropertyChanged(); }
    }

    public string StatusColor => IsOK ? "#10B981" : "#EF4444";
    public string StatusText => IsOK ? "OK" : "NG";

    public string Unit { get; set; } // "°C", "N", etc.

    public string DisplayValue => $"{CurrentValue:F1} {Unit}";

    private void ValidateAgainstSpec()
    {
        IsOK = CurrentValue >= MinSpec && CurrentValue <= MaxSpec;
    }

    public ParameterViewModel(string name, string id, double min, double max, string unit)
    {
        ParameterName = name;
        ParameterID = id;
        MinSpec = min;
        MaxSpec = max;
        Unit = unit;
    }
}
```

#### 6. **MeasurementViewModel.cs**

```csharp
public class MeasurementViewModel : ViewModelBase
{
    public string MeasurementName { get; set; } // "X", "Y", "Z", "Ø"

    private double _currentValue;
    public double CurrentValue
    {
        get => _currentValue;
        set
        {
            _currentValue = value;
            OnPropertyChanged();
            ValidateAgainstSpec();
        }
    }

    public double MinSpec { get; set; }
    public double MaxSpec { get; set; }

    private bool _isOK = true;
    public bool IsOK
    {
        get => _isOK;
        set { _isOK = value; OnPropertyChanged(); }
    }

    public string StatusColor => IsOK ? "#10B981" : "#EF4444";

    private void ValidateAgainstSpec()
    {
        IsOK = CurrentValue > MinSpec && CurrentValue < MaxSpec;
    }

    public MeasurementViewModel(string name, double min, double max)
    {
        MeasurementName = name;
        MinSpec = min;
        MaxSpec = max;
    }
}
```

---

## Data Binding Patterns

### In XAML - Parameter Binding Example

```xaml
<DataGrid ItemsSource="{Binding Stage1_UAT1.Parameters}">
    <DataGridTextColumn Header="Parameter" Binding="{Binding ParameterName}"/>
    <DataGridTextColumn Header="Value" Binding="{Binding CurrentValue, StringFormat='{0:F1}'}"/>
    <DataGridTextColumn Header="Status" Binding="{Binding StatusText}"/>
    <DataGridTextColumn Header="Spec" Binding="{Binding SpecDisplay}"/>
</DataGrid>
```

### In XAML - Stage Status Binding

```xaml
<Border Background="{Binding Stage1_UAT1.HeaderBackground, Converter={StaticResource StringToColorConverter}}">
    <TextBlock Text="{Binding Stage1_UAT1.StageIndicator}"/>
</Border>
```

---

## Real-time Data Updates

### Modbus Polling Implementation

```csharp
public class ModbusDataService
{
    private IModbusMaster _modbusMaster;
    private DispatcherTimer _updateTimer;

    public void StartPolling(BendingDashboardViewModel viewModel, int intervalMs = 500)
    {
        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMs) };
        _updateTimer.Tick += (s, e) => UpdateAllStages(viewModel);
        _updateTimer.Start();
    }

    private void UpdateAllStages(BendingDashboardViewModel viewModel)
    {
        // Read from Modbus and update:
        // Stage 1: T1-T4, P1-P4, X, Y, Z, Ø
        // Stage 2: T5-T8, P5-P8, X, Y, Z, Ø
        // Stage 3: T9-T12, P9-P12, X, Y, Z, Ø

        // Example:
        var t1Value = _modbusMaster.ReadCoil(0, 0); // Modbus address for T1
        viewModel.Stage1_UAT1.Parameters[0].CurrentValue = t1Value; // Auto-triggers validation
    }
}
```

---

## Required Converters

### 1. StatusToColorConverter
```csharp
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            "OK" => new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),      // Green
            "NG" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),         // Red
            "Active" => new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),   // Green
            "Pending" => new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)), // Gray
            _ => new SolidColorBrush(Color.FromArgb(255, 100, 116, 139))          // Dark Gray
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

### 2. StringToColorConverter
```csharp
public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Green" => new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
            "Blue" => new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)),
            "Gray" => new SolidColorBrush(Color.FromArgb(255, 107, 114, 128)),
            "Red" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
            _ => new SolidColorBrush(Color.FromArgb(255, 15, 23, 42))
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

---

## File Structure (XAML Location)

```
IPCSoftware.App.Bending/
├── Views/
│   ├── BendingDashboard.xaml
│   ├── Controls/
│   │   ├── HeaderControl.xaml
│   │   ├── LotTrackingControl.xaml
│   │   ├── TurnTableVisualizationControl.xaml
│   │   ├── StageColumnControl.xaml
│   │   │   ├── CurrentStepPanel.xaml
│   │   │   ├── ParametersPanel.xaml
│   │   │   ├── MeasurementsPanel.xaml
│   │   │   ├── ProgressPanel.xaml
│   │   │   └── ResultPanel.xaml
│   │   └── SystemMetricsBar.xaml
│   │
├── ViewModels/
│   ├── BendingDashboardViewModel.cs
│   ├── CurrentLotViewModel.cs
│   ├── LotViewModel.cs
│   ├── TurnTableViewModel.cs
│   ├── StageViewModel.cs
│   ├── ParameterViewModel.cs
│   └── MeasurementViewModel.cs
│
├── Services/
│   └── ModbusDataService.cs
│
└── Converters/
    ├── StatusToColorConverter.cs
    └── StringToColorConverter.cs
```

---

## Animation Reference (TurnTable Rotation)

```xaml
<Ellipse x:Name="TurnTableVisual" Width="120" Height="120">
    <Ellipse.RenderTransform>
        <RotateTransform x:Name="TurnTableRotation" CenterX="60" CenterY="60"/>
    </Ellipse.RenderTransform>
    <Ellipse.Triggers>
        <EventTrigger RoutedEvent="FrameworkElement.Loaded">
            <BeginStoryboard>
                <Storyboard RepeatBehavior="Forever">
                    <DoubleAnimation
                        Storyboard.TargetName="TurnTableRotation"
                        Storyboard.TargetProperty="Angle"
                        From="0" To="360"
                        Duration="0:0:8"/>
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </Ellipse.Triggers>
</Ellipse>
```

---

## Summary

This architecture provides:
✅ Clean separation of concerns (View, ViewModel, Model)
✅ Reactive data binding for real-time updates
✅ Reusable controls for the 3 stages
✅ Color and status converters for visual feedback
✅ Modbus integration point
✅ Scalable parameter handling

Follow this structure when converting the HTML prototype to WPF XAML.
