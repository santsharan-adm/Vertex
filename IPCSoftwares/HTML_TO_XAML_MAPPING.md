# HTML to XAML Mapping - Bending Machine HMI

This document maps each HTML component from the original prototype to its XAML equivalent, ensuring feature parity.

## Header Section

### HTML
```html
<div class="header">
    <div class="header-left">
        ⚙️ FLEX BENDING MACHINE
        <div class="status">● RUNNING</div>
    </div>
    <div class="header-right">
        Cycle: <strong id="cycleNum">001</strong> | Time: <strong id="cycleTime">0s</strong>
    </div>
</div>
```

### XAML Equivalent
```xaml
<Border DockPanel.Dock="Top" Background="#1E293B" BorderBrush="#3B82F6" BorderThickness="0,0,0,2" Padding="16,8">
    <DockPanel>
        <StackPanel Orientation="Horizontal" DockPanel.Dock="Left" VerticalAlignment="Center" Gap="12">
            <TextBlock Text="⚙️ FLEX BENDING MACHINE" FontWeight="Bold" FontSize="13" VerticalAlignment="Center"/>
            <Border Background="#10B981" Padding="4,4,12,4" CornerRadius="3">
                <TextBlock Text="● RUNNING" FontSize="10"/>
            </Border>
        </StackPanel>
        <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" VerticalAlignment="Center" Gap="16" FontSize="10">
            <TextBlock><Run Text="Cycle: "/><Run Text="{Binding CycleNumber}" FontWeight="Bold"/></TextBlock>
            <TextBlock><Run Text="Time: "/><Run Text="{Binding CycleTime}" FontWeight="Bold"/><Run Text="s"/></TextBlock>
        </StackPanel>
    </DockPanel>
</Border>
```

**Mapping Details:**
| HTML | XAML |
|------|------|
| `.header` div | `Border` with `DockPanel.Dock="Top"` |
| `.status` div | Inner `Border` with green background |
| `#cycleNum` | `Binding CycleNumber` |
| `#cycleTime` | `Binding CycleTime` |
| CSS colors | Direct hex color values |

---

## Top Section: Parameter Tiles

### HTML Structure
```html
<div class="top-section">
    <div class="param-tile">
        <div class="tile-title">🔴 BENDING 1 PARAMETERS</div>
        <div class="tile-stage">At TT-1 Position 1</div>
        <div class="tile-lot-id" id="bend1-lot">LOT: ---</div>
        <!-- Parameters and QR codes -->
    </div>
    <!-- ... Bending 2 and 3 tiles ... -->
</div>
```

### XAML Equivalent
```xaml
<Border DockPanel.Dock="Top" Background="#0F172A" Padding="8" Height="220">
    <Grid ColumnDefinitions="*,*,*" RowDefinitions="*" Gap="8">
        <!-- Three identical parameter tiles -->
        <Border Grid.Column="0" Background="#1E293B" BorderBrush="#3B82F6" BorderThickness="2" CornerRadius="6" Padding="12">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <StackPanel Gap="8">
                    <TextBlock Text="🔴 BENDING 1 PARAMETERS" FontSize="11" FontWeight="Bold" Foreground="#10B981"/>
                    <TextBlock Text="At TT-1 Position 1" FontSize="10" Foreground="#94A3B8"/>
                    <TextBlock Text="{Binding Bending1Parameters.LotId}" FontSize="14" FontWeight="Bold" Foreground="#3B82F6"/>
                    <!-- Temperature and Pressure columns -->
                    <!-- QR codes section -->
                </StackPanel>
            </ScrollViewer>
        </Border>
    </Grid>
</Border>
```

**Mapping Details:**
| HTML | XAML |
|------|------|
| `.top-section` | `Border` with `Grid` (3 columns) |
| `.param-tile` | `Border` with `ScrollViewer` |
| `.tile-title` | `TextBlock` with emoji |
| `.tile-stage` | `TextBlock` with secondary color |
| `.tile-lot-id` | `TextBlock` binding to `LotId` |
| Temperature inputs | 4x `Border` with `TextBlock` binding |
| Pressure inputs | 4x `Border` with `TextBlock` binding |
| QR codes | `StackPanel` with 4x `TextBlock` |

---

## Middle Section: TurnTables

### HTML
```html
<div class="turntable">
    <div class="tt-dot"></div>
    <!-- Position markers and labels -->
</div>
```

### XAML Equivalent
```xaml
<Canvas Width="120" Height="120" Background="Transparent" HorizontalAlignment="Center">
    <!-- TurnTable Circle -->
    <Ellipse Canvas.Left="0" Canvas.Top="0" Width="120" Height="120"
            Stroke="#3B82F6" StrokeThickness="3" Fill="Transparent"/>

    <!-- Position Indicators -->
    <Ellipse Canvas.Left="54" Canvas.Top="4" Width="12" Height="12"
            Stroke="#0F172A" StrokeThickness="2"
            Fill="{Binding TT1_Pos0_Active, Converter={StaticResource BoolToColorConverter}}"/>
    <!-- ... other positions ... -->
</Canvas>
```

**Mapping Details:**
| HTML | XAML |
|------|------|
| `.turntable` (circular div) | `Canvas` with `Ellipse` |
| `.tt-dot` (rotating indicator) | `Ellipse` with binding to `*_Active` properties |
| CSS rotation animation | Converter handles color (active=green, inactive=blue) |
| Position labels | Added as `TextBlock` within `Canvas` |

---

## Bottom Section: Tables

### HTML
```html
<div class="table-row">
    <div class="row-num">1</div>
    <div class="row-uid">LOT-001</div>
</div>
```

### XAML Equivalent
```xaml
<ListBox Background="Transparent" ItemsSource="{Binding ScannedPartsList}">
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem">
            <Setter Property="Background" Value="#0F172A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderBrush" Value="#3B82F6"/>
            <Setter Property="BorderThickness" Value="2,0,0,0"/>
            <Setter Property="Padding" Value="6"/>
            <Setter Property="Margin" Value="0,0,0,4"/>
        </Style>
    </ListBox.ItemContainerStyle>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding}" FontSize="9"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

**Mapping Details:**
| HTML | XAML |
|------|------|
| `.table-row` | `ListBoxItem` with custom style |
| Row iteration | `ItemsSource="{Binding ScannedPartsList}"` |
| Static HTML rows | Data binding to ObservableCollection |
| Styling | `ItemContainerStyle` for uniform appearance |

---

## Lot Movement Flow Comparison

### HTML JavaScript Implementation
```javascript
function moveLots() {
    // Transfer from TT-1 Pos 3 to TT-2 Pos 0
    if (systemState.tt1_pos3) {
        systemState.tt2_pos0 = systemState.tt1_pos3;
        systemState.tt1_pos3 = null;
    }
    // ... more movements ...
}

setInterval(() => {
    timeCounter += 2;
    if (timeCounter % 4 === 0) {
        moveLots();
    }
    updateDisplay();
}, 2000);
```

### XAML C# Implementation
```csharp
private void OnSimulationTick(object sender, ElapsedEventArgs e)
{
    _timeCounter += 2;
    CycleTime = _timeCounter;

    if (_timeCounter % 4 == 0)
    {
        MoveLots();
    }

    UpdateDisplay();
}

private void MoveLots()
{
    // Transfer from TT-1 Pos 3 to TT-2 Pos 0
    if (!string.IsNullOrEmpty(SystemState.TT1_Pos3))
    {
        SystemState.TT2_Pos0 = SystemState.TT1_Pos3;
        SystemState.TT1_Pos3 = null;
    }
    // ... more movements ...
}
```

**Key Differences:**
| Aspect | HTML | XAML |
|--------|------|------|
| Timing | `setInterval()` | `System.Timers.Timer` |
| State Storage | Plain object | Strongly-typed `SystemStateModel` |
| Updates | DOM manipulation | INotifyPropertyChanged binding |
| Data | Simulated in JavaScript | Managed in ViewModel |
| Thread Safety | HTML/JS (single-threaded) | C# with Timer (must dispatch to UI) |

---

## Data Binding Comparison

### HTML: Direct DOM Manipulation
```javascript
document.getElementById('bend1-lot').textContent = `LOT: ${lot.id}`;
document.getElementById('b1-t1').textContent = `Temp-1: ${lot.temp[0]}°C`;
```

### XAML: Two-Way Data Binding
```xaml
<TextBlock Text="{Binding Bending1Parameters.LotId}" />
<TextBlock Text="{Binding Bending1Parameters.Temp1, StringFormat='Temp-1: {0}°C'}" />
```

**Benefits of XAML Binding:**
- Automatic UI update on property change
- Type-safe (compile-time checking)
- Cleaner separation of concerns
- Support for converters and validation
- Built-in support for multi-source binding

---

## Animation Comparison

### HTML: CSS Animation
```html
<style>
    .turntable { animation: rotate 16s linear infinite; }
    @keyframes rotate { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
</style>
<div class="turntable"><div class="tt-dot"></div></div>
```

### XAML: Visual Indicators
```xaml
<Canvas Width="120" Height="120">
    <Ellipse Stroke="#3B82F6" StrokeThickness="3" Fill="Transparent"/>
    <Ellipse Canvas.Left="54" Canvas.Top="4" Width="12" Height="12"
            Fill="{Binding TT1_Pos0_Active, Converter={StaticResource BoolToColorConverter}}"/>
</Canvas>
```

**Note:** In XAML, TurnTable position indicators change color rather than rotate, as the physical rotation is less relevant in a discrete position system.

---

## JavaScript Functions to C# Methods Mapping

| JavaScript Function | C# Equivalent | Location |
|-------------------|---------------|----------|
| `getParameterValues()` | `LotManager.GetLot()` | LotManager.cs |
| `updateDisplay()` | `UpdateDisplay()` | MainViewModel.cs |
| `updateTTDisplay()` | `UpdateDisplay()` (partial) | MainViewModel.cs |
| `updateParameterTiles()` | `UpdateParameterTiles()` | MainViewModel.cs |
| `updateRobotDisplays()` | Property bindings | MainViewModel.cs |
| `updateTables()` | `AddProcessEvent()` | MainViewModel.cs |
| `moveLots()` | `MoveLots()` | MainViewModel.cs |
| (Array loop) | `for (int i = 1; i <= 10; i++)` | LotManager.cs |

---

## Color Palette Verification

### Matching Hex Values

| Element | HTML Hex | XAML Hex | Match |
|---------|----------|----------|-------|
| Background | #0F172A | #0F172A | ✓ |
| Panel | #1E293B | #1E293B | ✓ |
| Border | #3B82F6 | #3B82F6 | ✓ |
| Active Indicator | #10B981 | #10B981 | ✓ |
| Secondary Text | #94A3B8 | #94A3B8 | ✓ |
| Light Border | #CBD5E1 | #CBD5E1 | ✓ |

All colors match exactly between HTML and XAML implementations.

---

## UI Element Size Comparison

| Element | HTML Dimension | XAML Dimension | Notes |
|---------|---|---|---|
| Header Height | 40px | Auto | Font-based sizing |
| Top Section Height | 200px | 220px | Slight adjustment for XAML spacing |
| Middle Section Height | 280px | 320px | Adjusted for better proportions |
| TurnTable Size | 100x100px | 120x120px | Larger for better visibility |
| Font Sizes | 9-13px | 9-13pt | Consistent scaling |

---

## Verification Checklist

- [x] All HTML colors mapped to XAML
- [x] All data bindings configured
- [x] All 10 lots created with unique QR codes
- [x] Parameter tiles for 3 bending stages
- [x] TurnTable visualization with position indicators
- [x] Input/Output robot displays
- [x] Scanned parts table with data binding
- [x] Process events table with data binding
- [x] Cycle number and time display
- [x] Position status indicators
- [x] Lot movement logic preserved
- [x] Event logging system maintained
- [x] Timing mechanism (2s intervals, 4s movement)
- [x] Converter for position color indication

---

## Migration Notes

When migrating from HTML to XAML:
1. All JavaScript logic moved to MainViewModel.cs
2. All DOM manipulation replaced with data binding
3. All CSS styles converted to XAML properties
4. HTML IDs replaced with binding paths
5. Event handlers replaced with timers and async operations
6. Static data initialization moved to LotManager.cs
7. UI updates now automatic through PropertyChanged notifications

This ensures complete feature parity while leveraging WPF's type-safe, binding-based architecture.
