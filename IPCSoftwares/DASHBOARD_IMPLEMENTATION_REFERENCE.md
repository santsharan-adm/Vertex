# Bending Machine Dashboard - Implementation Reference

## Quick Start: Key Components to Implement

### 1. Enhanced Header Component

Create a new reusable header control:

**HeaderControl.xaml**
```xaml
<UserControl x:Class="IPCSoftware.App.Bending.Controls.HeaderControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Height="120" Background="#1E293B" BorderBrush="#3B82F6" BorderThickness="0,0,0,3">

    <Grid Padding="20">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Left: Title and Status -->
        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
            <TextBlock Text="⚙️ FLEX BENDING MACHINE"
                      FontSize="24" FontWeight="Bold" Foreground="White"/>
            <Border Background="#10B981" Margin="20,0,0,0" Padding="10,4"
                   CornerRadius="6" VerticalAlignment="Center">
                <StackPanel Orientation="Horizontal" Gap="8">
                    <Ellipse Width="6" Height="6" Fill="White"
                            Name="StatusDot"/>
                    <TextBlock Text="{Binding MachineStatus}"
                              Foreground="White" FontWeight="Bold" FontSize="11"/>
                </StackPanel>
            </Border>
        </StackPanel>

        <!-- Right: KPI Cards -->
        <UniformGrid Grid.Column="1" Columns="4" ColumnWidth="140" HorizontalSpacing="10">
            <!-- KPI Card 1: Cycle Time -->
            <Border BorderBrush="#CBD5E1" BorderThickness="1"
                   Background="#0F172A" CornerRadius="8" Padding="12">
                <StackPanel>
                    <TextBlock Text="CYCLE TIME" FontSize="9"
                              Foreground="#94A3B8" FontWeight="Bold" TextAlignment="Center"/>
                    <TextBlock Text="{Binding CycleTime, StringFormat='{0:F1}s'}"
                              FontSize="20" Foreground="White" FontWeight="Bold"
                              TextAlignment="Center" Margin="0,4,0,0"/>
                </StackPanel>
            </Border>

            <!-- KPI Card 2: Efficiency -->
            <Border BorderBrush="#CBD5E1" BorderThickness="1"
                   Background="#0F172A" CornerRadius="8" Padding="12">
                <StackPanel>
                    <TextBlock Text="EFFICIENCY" FontSize="9"
                              Foreground="#94A3B8" FontWeight="Bold" TextAlignment="Center"/>
                    <TextBlock Text="{Binding Efficiency, StringFormat='{0:F0}%'}"
                              FontSize="20" Foreground="#10B981" FontWeight="Bold"
                              TextAlignment="Center" Margin="0,4,0,0"/>
                </StackPanel>
            </Border>

            <!-- KPI Card 3: OEE -->
            <Border BorderBrush="#CBD5E1" BorderThickness="1"
                   Background="#0F172A" CornerRadius="8" Padding="12">
                <StackPanel>
                    <TextBlock Text="OEE SCORE" FontSize="9"
                              Foreground="#94A3B8" FontWeight="Bold" TextAlignment="Center"/>
                    <TextBlock Text="{Binding OEEScore, StringFormat='{0:F0}%'}"
                              FontSize="20" Foreground="#10B981" FontWeight="Bold"
                              TextAlignment="Center" Margin="0,4,0,0"/>
                </StackPanel>
            </Border>

            <!-- KPI Card 4: Defect Rate -->
            <Border BorderBrush="#CBD5E1" BorderThickness="1"
                   Background="#0F172A" CornerRadius="8" Padding="12">
                <StackPanel>
                    <TextBlock Text="DEFECTS" FontSize="9"
                              Foreground="#94A3B8" FontWeight="Bold" TextAlignment="Center"/>
                    <TextBlock Text="{Binding DefectRate, StringFormat='{0:F1}%'}"
                              FontSize="20" Foreground="#EF4444" FontWeight="Bold"
                              TextAlignment="Center" Margin="0,4,0,0"/>
                </StackPanel>
            </Border>
        </UniformGrid>
    </Grid>
</UserControl>
```

**HeaderControl.xaml.cs**
```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace IPCSoftware.App.Bending.Controls
{
    public partial class HeaderControl : UserControl
    {
        public HeaderControl()
        {
            InitializeComponent();
            AnimateStatusDot();
        }

        private void AnimateStatusDot()
        {
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.3,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            };

            StatusDot.BeginAnimation(OpacityProperty, animation);
        }
    }
}
```

---

### 2. KPI Card Component

**KPICard.xaml**
```xaml
<UserControl x:Class="IPCSoftware.App.Bending.Controls.KPICard"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="Transparent">

    <Border BorderBrush="{Binding BorderColor}" BorderThickness="1"
           Background="#1E293B" CornerRadius="12" Padding="20" Cursor="Hand">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- Label -->
            <TextBlock Grid.Row="0" Text="{Binding Label}"
                      FontSize="11" Foreground="#94A3B8"
                      FontWeight="Bold" TextAlignment="Center"/>

            <!-- Value -->
            <TextBlock Grid.Row="1" Text="{Binding Value}"
                      FontSize="32" Foreground="{Binding ValueColor}"
                      FontWeight="Bold" TextAlignment="Center" Margin="0,8,0,8"/>

            <!-- Trend -->
            <TextBlock Grid.Row="2" Text="{Binding TrendText}"
                      FontSize="12" Foreground="{Binding TrendColor}"
                      TextAlignment="Center"/>
        </Grid>
    </Border>
</UserControl>
```

---

### 3. Alert System Component

**AlertPanel.xaml**
```xaml
<UserControl x:Class="IPCSoftware.App.Bending.Controls.AlertPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Border Background="#1E293B" BorderBrush="#CBD5E1" BorderThickness="1" CornerRadius="12">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Header -->
            <Border Grid.Row="0" Background="#3B82F6" Padding="16">
                <TextBlock Text="🔔 SYSTEM ALERTS" Foreground="White"
                          FontWeight="Bold" FontSize="12"/>
            </Border>

            <!-- Alerts List -->
            <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto" MaxHeight="300">
                <ItemsControl ItemsSource="{Binding Alerts}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border BorderBrush="#0F172A" BorderThickness="0,0,0,1"
                                   Padding="16" Margin="0">
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>

                                    <!-- Icon -->
                                    <Border Grid.Column="0" Width="32" Height="32"
                                           CornerRadius="50%" VerticalAlignment="Top"
                                           Background="{Binding SeverityColor}">
                                        <TextBlock Text="{Binding Icon}"
                                                  Foreground="White" FontSize="16"
                                                  HorizontalAlignment="Center"
                                                  VerticalAlignment="Center"/>
                                    </Border>

                                    <!-- Content -->
                                    <StackPanel Grid.Column="1" Margin="12,0,0,0">
                                        <TextBlock Text="{Binding Title}"
                                                  Foreground="White" FontWeight="Bold"
                                                  FontSize="12"/>
                                        <TextBlock Text="{Binding Message}"
                                                  Foreground="#94A3B8" FontSize="11"
                                                  TextWrapping="Wrap" Margin="0,4,0,0"/>
                                        <TextBlock Text="{Binding TimeAgo}"
                                                  Foreground="#64748B" FontSize="9"
                                                  Margin="0,2,0,0"/>
                                    </StackPanel>

                                    <!-- Close Button -->
                                    <Button Grid.Column="2" Content="✕"
                                           Foreground="#94A3B8" FontSize="14"
                                           Background="Transparent" BorderThickness="0"
                                           VerticalAlignment="Top" Cursor="Hand"
                                           Command="{Binding ElementName=AlertPanel,
                                                   Path=DataContext.DismissAlertCommand}"
                                           CommandParameter="{Binding}"/>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
        </Grid>
    </Border>
</UserControl>
```

---

### 4. Data Table Component (Reusable)

**DataGridColumn Style**
```xaml
<UserControl.Resources>
    <Style x:Key="DataGridHeaderStyle" TargetType="DataGridColumnHeader">
        <Setter Property="Background" Value="#0F172A"/>
        <Setter Property="Foreground" Value="#94A3B8"/>
        <Setter Property="Padding" Value="12,8"/>
        <Setter Property="FontSize" Value="10"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="BorderBrush" Value="Transparent"/>
    </Style>

    <Style TargetType="DataGridCell">
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="12,8"/>
        <Setter Property="BorderThickness" Value="0"/>
    </Style>

    <Style TargetType="DataGridRow">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Trigger Property="ItemsControl.AlternationIndex" Value="0">
            <Setter Property="Background" Value="#1E293B"/>
        </Trigger>
    </Style>
</UserControl.Resources>

<!-- Data Grid -->
<DataGrid ItemsSource="{Binding PartData}"
         AutoGenerateColumns="False"
         CanUserAddRows="False"
         GridLinesVisibility="None"
         HeadersVisibility="Column"
         Background="#0F172A"
         ColumnHeaderStyle="{StaticResource DataGridHeaderStyle}">

    <DataGrid.Columns>
        <DataGridTextColumn Header="Part No." Binding="{Binding PartNumber}" Width="100"/>
        <DataGridTextColumn Header="Temperature (°C)" Binding="{Binding Temperature}" Width="*"/>
        <DataGridTextColumn Header="Load (N)" Binding="{Binding Load}" Width="*"/>
        <DataGridTemplateColumn Header="Status" Width="80">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <Border Background="{Binding StatusColor}"
                           CornerRadius="4" Padding="8,2" HorizontalAlignment="Center">
                        <TextBlock Text="{Binding Status}"
                                  Foreground="White" FontWeight="Bold" FontSize="10"/>
                    </Border>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

---

### 5. Process Flow Component

**ProcessFlowControl.xaml**
```xaml
<UserControl x:Class="IPCSoftware.App.Bending.Controls.ProcessFlowControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Border Background="#1E293B" BorderBrush="#CBD5E1" BorderThickness="1" CornerRadius="12" Padding="16">
        <StackPanel>
            <!-- Title -->
            <TextBlock Text="📊 PROCESS FLOW" FontWeight="Bold" FontSize="12"
                      Foreground="White" Margin="0,0,0,16"/>

            <!-- Flow Items -->
            <ItemsControl ItemsSource="{Binding ProcessStages}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border BorderBrush="{Binding BorderColor}" BorderThickness="1"
                               Background="{Binding BackgroundColor}" CornerRadius="8"
                               Padding="12" Margin="0,0,0,8">
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="Auto"/>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>

                                <!-- Stage Indicator Dots -->
                                <StackPanel Grid.Column="0" Orientation="Horizontal" Gap="4">
                                    <Ellipse Width="8" Height="8" Fill="{Binding DotColor1}"/>
                                    <Ellipse Width="8" Height="8" Fill="{Binding DotColor2}"/>
                                    <Ellipse Width="8" Height="8" Fill="{Binding DotColor3}"/>
                                    <Ellipse Width="8" Height="8" Fill="{Binding DotColor4}"/>
                                </StackPanel>

                                <!-- Stage Name -->
                                <TextBlock Grid.Column="1" Text="{Binding StageName}"
                                          Foreground="White" FontSize="11"
                                          Margin="12,0,0,0" VerticalAlignment="Center"/>

                                <!-- Time -->
                                <TextBlock Grid.Column="2" Text="{Binding ElapsedTime}"
                                          Foreground="#94A3B8" FontSize="9"
                                          VerticalAlignment="Center"/>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </StackPanel>
    </Border>
</UserControl>
```

---

### 6. ViewModel Structure for Dashboard

**DashboardViewModel.cs**
```csharp
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using IPCSoftware.App.Bending.Models;

namespace IPCSoftware.App.Bending.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        // Real-time Properties
        private string _machineStatus = "RUNNING";
        public string MachineStatus
        {
            get => _machineStatus;
            set { _machineStatus = value; OnPropertyChanged(); }
        }

        private double _cycleTime;
        public double CycleTime
        {
            get => _cycleTime;
            set { _cycleTime = value; OnPropertyChanged(); }
        }

        private double _efficiency;
        public double Efficiency
        {
            get => _efficiency;
            set { _efficiency = value; OnPropertyChanged(); }
        }

        private double _oeeScore;
        public double OEEScore
        {
            get => _oeeScore;
            set { _oeeScore = value; OnPropertyChanged(); }
        }

        private double _defectRate;
        public double DefectRate
        {
            get => _defectRate;
            set { _defectRate = value; OnPropertyChanged(); }
        }

        // Collections
        public ObservableCollection<AlertViewModel> Alerts { get; private set; }
        public ObservableCollection<StageViewModel> ProcessStages { get; private set; }
        public ObservableCollection<UATDataViewModel> UATData { get; private set; }

        // Commands
        public ICommand StartCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand NextStageCommand { get; private set; }

        public DashboardViewModel()
        {
            Alerts = new ObservableCollection<AlertViewModel>();
            ProcessStages = new ObservableCollection<StageViewModel>();
            UATData = new ObservableCollection<UATDataViewModel>();

            InitializeCommands();
            InitializeData();
            StartMonitoring();
        }

        private void InitializeCommands()
        {
            StartCommand = new RelayCommand(() => HandleStart());
            PauseCommand = new RelayCommand(() => HandlePause());
            NextStageCommand = new RelayCommand(() => HandleNextStage());
        }

        private void InitializeData()
        {
            // Load initial data from service
            // This would typically come from IPCSoftware.CoreService
        }

        private void StartMonitoring()
        {
            // Start timer to update real-time data
            var timer = new System.Timers.Timer(500); // Update every 500ms
            timer.Elapsed += (s, e) => UpdateRealTimeData();
            timer.Start();
        }

        private void UpdateRealTimeData()
        {
            // Fetch data from hardware service
            // Update properties
        }

        private void HandleStart()
        {
            MachineStatus = "RUNNING";
            // Send command to machine service
        }

        private void HandlePause()
        {
            MachineStatus = "PAUSED";
            // Send command to machine service
        }

        private void HandleNextStage()
        {
            // Advance to next stage
        }

        public void AddAlert(AlertViewModel alert)
        {
            Alerts.Insert(0, alert);

            // Auto-remove info alerts after 5 seconds
            if (alert.Severity == AlertSeverity.Info)
            {
                var timer = new System.Timers.Timer(5000);
                timer.Elapsed += (s, e) =>
                {
                    Alerts.Remove(alert);
                    timer.Stop();
                };
                timer.Start();
            }
        }
    }

    public class AlertViewModel : ViewModelBase
    {
        public enum AlertSeverity { Critical, Warning, Info }

        public AlertSeverity Severity { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; }

        public string SeverityColor => Severity switch
        {
            AlertSeverity.Critical => "#EF4444",
            AlertSeverity.Warning => "#F59E0B",
            AlertSeverity.Info => "#3B82F6",
            _ => "#94A3B8"
        };

        public string TimeAgo =>
            (DateTime.Now - Timestamp).TotalSeconds < 60
                ? $"{(int)(DateTime.Now - Timestamp).TotalSeconds}s ago"
                : $"{(int)(DateTime.Now - Timestamp).TotalMinutes}m ago";
    }

    public class StageViewModel : ViewModelBase
    {
        public string StageName { get; set; }
        public bool IsActive { get; set; }
        public bool IsComplete { get; set; }
        public TimeSpan ElapsedTime { get; set; }

        public string BorderColor => IsActive ? "#3B82F6" : IsComplete ? "#10B981" : "#CBD5E1";
        public string BackgroundColor => IsActive ? "#0F172A" : "#1E293B";
    }

    public class UATDataViewModel : ViewModelBase
    {
        public string StationName { get; set; }
        public double CurrentTemperature { get; set; }
        public double CurrentLoad { get; set; }
        public string Status { get; set; }
    }
}
```

---

## Color Converter Utilities

**StatusToColorConverter.cs**
```csharp
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IPCSoftware.App.Bending.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString();
            return status switch
            {
                "OK" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                "NG" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                "RUNNING" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                "PAUSED" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                "ERROR" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class TemperatureToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString(), out var temp))
            {
                return temp > 250 ? "⚠️ Critical" : temp > 240 ? "⚡ Warning" : "✅ Normal";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
```

---

## Integration Checklist

- [ ] Create header control with KPI cards
- [ ] Create alert panel with severity levels
- [ ] Create process flow visualization
- [ ] Create reusable data grid template
- [ ] Implement DashboardViewModel with commands
- [ ] Create status color converters
- [ ] Connect to hardware service for real-time data
- [ ] Add animations for turn tables
- [ ] Implement data refresh timer
- [ ] Test responsive layout on different screen sizes
- [ ] Add sound alerts for critical errors (optional)
- [ ] Implement data logging for analytics

---

## Common Issues & Solutions

### Issue 1: UI Freezes During Data Updates
**Solution**: Use background thread for data fetching
```csharp
Task.Run(() => {
    var data = FetchHardwareData();
    Application.Current.Dispatcher.Invoke(() => {
        UpdateUI(data);
    });
});
```

### Issue 2: Binding Updates Too Slow
**Solution**: Implement throttling/debouncing
```csharp
private DispatcherTimer _updateTimer;

public void StartMonitoring()
{
    _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
    _updateTimer.Tick += (s, e) => UpdateRealTimeData();
    _updateTimer.Start();
}
```

### Issue 3: Memory Leaks in ObservableCollections
**Solution**: Clear collections when view unloads
```csharp
private void MainWindow_Closing(object sender, CancelEventArgs e)
{
    Alerts.Clear();
    ProcessStages.Clear();
    UATData.Clear();
}
```

---

## Testing the Dashboard

### Unit Test Example
```csharp
[TestClass]
public class DashboardViewModelTests
{
    [TestMethod]
    public void Efficiency_UpdatesCorrectly()
    {
        var vm = new DashboardViewModel();
        vm.Efficiency = 94.5;

        Assert.AreEqual(94.5, vm.Efficiency);
    }

    [TestMethod]
    public void AlertAdded_AppearInCollection()
    {
        var vm = new DashboardViewModel();
        var alert = new AlertViewModel
        {
            Title = "Test",
            Message = "Test Alert",
            Severity = AlertViewModel.AlertSeverity.Info
        };

        vm.AddAlert(alert);

        Assert.AreEqual(1, vm.Alerts.Count);
    }
}
```

---

## Performance Tips

1. **Use Virtualization**: For large lists, enable virtual panel scrolling
2. **Async Operations**: Load data asynchronously to keep UI responsive
3. **Throttle Updates**: Don't update UI faster than 20 times per second
4. **Unsubscribe Events**: Always unsubscribe from events in Dispose
5. **Lazy Loading**: Load detail data only when needed

---

## Additional Resources

- [Microsoft WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf)
- [MVVM Light Toolkit](https://www.mvvmlight.net/)
- [WPF Performance Optimization](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/optimizing-performance-overview)
