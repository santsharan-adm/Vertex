# Bending Machine HMI - WPF XAML Implementation Guide

## Overview
This document describes the WPF XAML implementation of the Bending Machine HMI (Human Machine Interface) following the MVVM (Model-View-ViewModel) architectural pattern.

## Project Structure

```
IPCSoftware.App.Bending/
├── Models/
│   ├── LotModel.cs                 # Data model for manufacturing lots
│   ├── SystemStateModel.cs         # System state and position tracking
│   ├── BindingParametersModel.cs   # Parameter display binding model
│   └── LotManager.cs               # Factory for lot creation
├── ViewModels/
│   ├── BaseViewModel.cs            # Base class with INotifyPropertyChanged
│   └── MainViewModel.cs            # Main application logic and simulation
├── Views/
│   ├── MainWindow.xaml             # Main UI layout
│   └── MainWindow.xaml.cs          # Code-behind
├── Converters/
│   └── BoolToColorConverter.cs     # Boolean to Color converter for indicators
├── App.xaml                        # Application resources
├── App.xaml.cs                     # Application code-behind
└── IPCSoftware.App.Bending.csproj # Project file
```

## Key Components

### 1. Models Layer

#### LotModel.cs
- Represents a manufacturing lot with unique ID
- Contains 4 QR codes per lot for tracking
- Stores temperature and pressure parameters (4 values each)
- Tracks creation time and processing status

#### SystemStateModel.cs
- Central state management for all TurnTable positions
- TT-1: 4 positions (Entry, Bending1, Bending2, Transfer)
- TT-2: 3 positions (Receive, Bending3, Exit)
- Tracks completed lots and process events
- Maintains cycle counter and time tracking

#### BindingParametersModel.cs
- Real-time parameter display for each bending stage
- Binds to UI elements with INotifyPropertyChanged
- Updates automatically when lot moves to a new position
- Displays temperature, pressure, and QR codes

#### LotManager.cs
- Static factory for creating 10 pre-configured lots
- Generates unique QR codes for each lot (ABCD{i}2345A{j})
- Creates varied temperature and pressure values based on lot number
- Provides singleton-like access to lot data

### 2. ViewModels Layer

#### BaseViewModel.cs
- Base class for all ViewModels
- Implements INotifyPropertyChanged interface
- Provides SetProperty helper for change tracking
- Enables two-way data binding

#### MainViewModel.cs
**Responsibilities:**
- Manages all UI state and logic
- Orchestrates lot movement simulation
- Updates parameter displays based on position changes
- Logs process events
- Manages timer-based simulation (2-second intervals)

**Key Methods:**
- `InitializeViewModel()`: Sets up initial state
- `StartSimulation()`: Begins the 2-second timer
- `OnSimulationTick()`: Executes every 2 seconds, moves lots every 4 seconds
- `MoveLots()`: Orchestrates lot flow through positions
- `UpdateDisplay()`: Refreshes all UI bindings
- `UpdateParameterTiles()`: Updates parameter displays based on lot position
- `AddProcessEvent()`: Logs events to process event list

**Lot Movement Logic:**
1. TT-2 Pos 1 → Pos 2 (Exit)
2. TT-2 Pos 0 → Pos 1 (Bending 3)
3. TT-1 Pos 2 → Pos 3 (Transfer)
4. TT-1 Pos 1 → Pos 2 (Bending 2)
5. TT-1 Pos 0 → Pos 1 (Bending 1)
6. New lot enters at Pos 0

### 3. Views Layer

#### MainWindow.xaml
**Layout Structure:**
- **Header**: Machine status, cycle number, timer
- **Top Section**: 3 parameter tiles (Bending 1, 2, 3)
- **Middle Section**: Input Robot, TT-1, Transfer arrow, TT-2, Output Robot
- **Bottom Section**: Scanned Parts table, Process Events table

**Key UI Elements:**
- Parameter Tiles: Display temperature (4 values), pressure (4 values), and QR codes
- TurnTables: Canvas-based circles with position indicators
- Position Indicators: Change color based on lot presence
- Tables: ListBox controls with data binding
- Real-time Updates: All elements bound to ViewModel properties

#### MainWindow.xaml.cs
- Initializes MainViewModel on window load
- Handles window shutdown and ViewModel cleanup
- Manages application lifecycle

### 4. Converters

#### BoolToColorConverter.cs
- Converts boolean position active state to visual color
- Active position (lot present): Green (#10B981)
- Inactive position: Blue (#3B82F6)
- Used in Canvas Ellipse fill bindings

## Data Flow

```
LotManager (Static Factory)
    ↓
MainViewModel.InitializeViewModel()
    ↓
Create SystemState, Parameters, Lots
    ↓
UI Bindings (Two-way)
    ↓
Update Display (every 2 seconds)
    ↓
Move Lots (every 4 seconds)
    ↓
Update Parameter Tiles
    ↓
Refresh All Bindings
```

## Binding Examples

### Parameter Display
```xaml
<TextBlock Text="{Binding Bending1Parameters.Temp1, StringFormat='Temp-1: {0}°C'}" />
```

### Position Indicators
```xaml
<Ellipse Fill="{Binding TT1_Pos0_Active, Converter={StaticResource BoolToColorConverter}}" />
```

### List Data
```xaml
<ListBox ItemsSource="{Binding ProcessEventsList}" />
```

## Simulation Details

### Timing
- **Update Interval**: 2 seconds
- **Movement Interval**: Every 4 seconds (2 update cycles)
- **Lot Count**: 10 lots (001-010)

### Process Events
- Entry: Lot scanned and enters TT-1 Pos 0
- Bending 1 Start: Lot moves to TT-1 Pos 1
- Bending 2 Start: Lot moves to TT-1 Pos 2
- Transfer Done: Lot moves from TT-1 Pos 3 to TT-2 Pos 0
- Bending 3 Start: Lot moves to TT-2 Pos 1
- Exit: Lot completes at TT-2 Pos 2

### Parameter Variations
- Temperature: Values range from 80-84°C with lot-specific offsets
- Pressure: Values range from 6.8-7.2 N with lot-specific variations
- Each lot has unique QR codes: ABCD{i}2345A{j}

## Color Scheme

| Element | Color | Hex Code |
|---------|-------|----------|
| Background | Dark Blue | #0F172A |
| Panel | Dark Slate | #1E293B |
| Border | Blue | #3B82F6 |
| Active | Green | #10B981 |
| Text Secondary | Gray | #94A3B8 |
| Light Border | Light Gray | #CBD5E1 |

## Build & Run Instructions

### Prerequisites
- .NET 6.0 SDK or later
- Visual Studio 2022 or Visual Studio Code with C# extension

### Building
```bash
dotnet build IPCSoftware.App.Bending.csproj
```

### Running
```bash
dotnet run --project IPCSoftware.App.Bending.csproj
```

### Building Executable
```bash
dotnet publish IPCSoftware.App.Bending.csproj -c Release
```

## Integration with Modbus (Future)

The current implementation uses simulated data. To integrate real Modbus data:

1. Install NModbus4 NuGet package
2. Create `Services/ModbusService.cs`
3. Implement Modbus polling in MainViewModel
4. Replace simulated parameter values with Modbus reads
5. Create ModbusConnection configuration section

### Example Modbus Integration Point
```csharp
// Replace simulated parameter update with:
var modbusService = new ModbusService("192.168.1.100", 502);
lot.Temperature[0] = modbusService.ReadTemperature(1);
lot.Pressure[0] = modbusService.ReadPressure(1);
```

## Performance Considerations

- **Memory**: Fixed 10 lots with static data allocation
- **CPU**: Minimal usage due to simple timer-based updates
- **UI Thread**: All updates on UI thread (safe with WPF dispatcher)
- **Scalability**: Can handle larger lot counts with observer pattern optimization

## Testing Checklist

- [ ] All 10 lots cycle through correctly
- [ ] Parameter tiles update when lots move to positions
- [ ] QR codes display correctly for active lots
- [ ] Process events log in real-time
- [ ] Robot displays show entering/exiting lots
- [ ] Position indicators highlight correctly
- [ ] Application closes cleanly
- [ ] No memory leaks after extended running

## Future Enhancements

1. **Modbus Integration**: Real data from PLC
2. **Data Logging**: Export process history to database
3. **Alarm System**: Visual/audio alerts for anomalies
4. **Recipe Management**: Store and load different bending parameters
5. **OEE Calculation**: Real-time efficiency metrics
6. **Multi-language Support**: Localization for different languages
7. **Touch Screen Optimization**: Larger buttons for industrial environment
8. **Network Monitoring**: Real-time connection status display

## Troubleshooting

### UI not updating
- Verify all properties use `SetProperty()` in ViewModel
- Check XAML binding paths match property names
- Ensure converter is registered in App.xaml

### Lot not moving
- Verify timer is started in MainViewModel constructor
- Check MoveLots() logic for correct position flow
- Confirm nextLotToEnter is not null

### Application crashes on close
- MainViewModel.Shutdown() must be called in MainWindow.OnClosed
- Timer must be properly disposed

## Code Style Guidelines

- Use XAML Grid/DockPanel for layout (no hard-coded positions)
- Implement all UI properties in ViewModel with binding
- Use consistent color hex codes from color scheme
- Name properties in ViewModel matching UI binding paths
- Document complex binding logic with comments
- Keep ViewModels focused on logic, not UI rendering

## Related Documentation

- Original HTML Prototype: `hmi_data_flow.html`
- User Sketches: Reference drawings for layout
- AOI Running Final: Similar MVVM implementation for AOI machine
