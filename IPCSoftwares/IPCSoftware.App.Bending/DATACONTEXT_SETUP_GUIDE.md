# Setting Up DataContext in Refactored MVVM Application

## Problem We Solved
The original code-behind instantiated ViewModels directly:
```csharp
public Dashboard()
{
    InitializeComponent();
    this.DataContext = new DashboardViewModel(); // ? Bad
}
```

This violates MVVM principles and makes testing impossible.

---

## Solution 1: XAML-Based DataContext (Recommended)

### In Dashboard.xaml:
```xaml
<UserControl x:Class="IPCSoftware.App.Bending.Views.Dashboard"
             xmlns="..."
       xmlns:local="clr-namespace:IPCSoftware.App.Bending"
             xmlns:vm="clr-namespace:IPCSoftware.App.Bending.ViewModels">
    
    <UserControl.DataContext>
 <vm:MainViewModel />
    </UserControl.DataContext>
    
    <!-- Rest of XAML -->
</UserControl>
```

**Pros:**
- ? Declarative (Designer-friendly)
- ? No code-behind required
- ? Visual Studio preview works

**Cons:**
- ? Hard to mock in tests
- ? ViewModel always new instance

---

## Solution 2: App.xaml DataContext Factory (Better)

### In App.xaml:
```xaml
<Application x:Class="IPCSoftware.App.Bending.App"
   xmlns="..."
        xmlns:local="clr-namespace:IPCSoftware.App.Bending"
     xmlns:vm="clr-namespace:IPCSoftware.App.Bending.ViewModels"
        xmlns:v="clr-namespace:IPCSoftware.App.Bending.Views">
    
  <Application.Resources>
    <vm:MainViewModel x:Key="MainViewModelResource" />
        
        <DataTemplate DataType="{x:Type vm:MainViewModel}">
            <v:Dashboard />
        </DataTemplate>
</Application.Resources>
</Application>
```

### In MainWindow.xaml:
```xaml
<Window x:Class="IPCSoftware.App.Bending.MainWindow"
    xmlns="..."
   xmlns:vm="clr-namespace:IPCSoftware.App.Bending.ViewModels">
    
    <Window.DataContext>
    <vm:MainViewModel />
    </Window.DataContext>
    
    <Grid>
<Views:UserControl1 />
    </Grid>
</Window>
```

**Pros:**
- ? Declarative setup
- ? Type-based DataTemplate routing
- ? Separation of concerns

**Cons:**
- ? Still not easily testable

---

## Solution 3: Dependency Injection (Best Practice)

### Create Service Container in App.xaml.cs:

```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace IPCSoftware.App.Bending
{
    public partial class App : Application
    {
   private ServiceProvider _serviceProvider;

   protected override void OnStartup(StartupEventArgs e)
        {
      // Setup DI container
       var services = new ServiceCollection();
   
  // Register ViewModels
         services.AddSingleton<MainViewModel>();
            services.AddSingleton<DashboardViewModel>();
            
     // Register Views
         services.AddSingleton<MainWindow>();
    services.AddSingleton<Dashboard>();
     
            _serviceProvider = services.BuildServiceProvider();
          
   // Create and show main window
       var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
         
  base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
    _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
```

### In MainWindow.xaml.cs:
```csharp
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending
{
    public partial class MainWindow : Window
    {
   public MainWindow(MainViewModel viewModel)
        {
         InitializeComponent();
            DataContext = viewModel; // ? Injected
}
    }
}
```

### In Dashboard.xaml.cs:
```csharp
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    public partial class Dashboard : UserControl
    {
        public Dashboard(MainViewModel viewModel)
      {
            InitializeComponent();
      DataContext = viewModel; // ? Injected
        }
    }
}
```

**Pros:**
- ? Fully testable with mocks
- ? Loose coupling
- ? Easy to swap implementations
- ? Centralizes object creation
- ? Industry standard

**Cons:**
- ?? Requires additional NuGet package (Microsoft.Extensions.DependencyInjection)
- ?? Slightly more code

---

## Solution 4: Simple Factory Pattern

### Create ViewModelLocator:

```csharp
namespace IPCSoftware.App.Bending.ViewModels
{
    public class ViewModelLocator
    {
        private static MainViewModel _mainViewModel;
        
        public static MainViewModel MainViewModel
 {
        get
        {
     if (_mainViewModel == null)
      {
          _mainViewModel = new MainViewModel();
            }
            return _mainViewModel;
            }
     }
    }
}
```

### In App.xaml:
```xaml
<Application ...
     xmlns:vm="clr-namespace:IPCSoftware.App.Bending.ViewModels">
    
    <Application.Resources>
        <vm:ViewModelLocator x:Key="ViewModelLocator" />
    </Application.Resources>
</Application>
```

### In MainWindow.xaml:
```xaml
<Window ...
        DataContext="{Binding Source={StaticResource ViewModelLocator}, Path=MainViewModel}">
    ...
</Window>
```

**Pros:**
- ? Simple to implement
- ? No external dependencies
- ? Singleton management

**Cons:**
- ? Static dependencies hard to test
- ? Global state

---

## Recommended Setup for Your Project

### Step 1: Install NuGet Package
```bash
dotnet add package Microsoft.Extensions.DependencyInjection
```

### Step 2: Update App.xaml.cs
```csharp
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using IPCSoftware.App.Bending.ViewModels;
using IPCSoftware.App.Bending.Views;

namespace IPCSoftware.App.Bending
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
   {
   var services = new ServiceCollection();
          
   // Register ViewModels as singletons
          services.AddSingleton<MainViewModel>();
            services.AddSingleton<DashboardViewModel>();
      
     // Register Views
 services.AddSingleton<MainWindow>();
    services.AddSingleton<Dashboard>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

    protected override void OnStartup(StartupEventArgs e)
        {
       var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
    base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
     {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
```

### Step 3: Update App.xaml
```xaml
<Application x:Class="IPCSoftware.App.Bending.App"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:local="clr-namespace:IPCSoftware.App.Bending"
           xmlns:converters="clr-namespace:IPCSoftware.App.Bending.Converters">
    
    <Application.Resources>
      <!-- Global Converters -->
        <converters:BoolToColorConverter x:Key="BoolToColorConverter"/>
    </Application.Resources>
</Application>
```

### Step 4: Update MainWindow.xaml.cs
```csharp
using System.Windows;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
 DataContext = viewModel;
     }
    }
}
```

### Step 5: Update Dashboard.xaml.cs
```csharp
using System.Windows.Controls;
using IPCSoftware.App.Bending.ViewModels;

namespace IPCSoftware.App.Bending.Views
{
    public partial class Dashboard : UserControl
    {
        public Dashboard(MainViewModel viewModel)
        {
            InitializeComponent();
     DataContext = viewModel;
        }
    }
}
```

---

## Testing with Dependency Injection

### Unit Test Example:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using IPCSoftware.App.Bending.ViewModels;
using IPCSoftware.App.Bending.Views;

[TestClass]
public class DashboardTests
{
    [TestMethod]
    public void Dashboard_BindsToMainViewModel()
    {
        // Arrange
   var mockViewModel = new Mock<MainViewModel>();
        var dashboard = new Dashboard(mockViewModel.Object);
        
   // Act
        var result = dashboard.DataContext;
        
        // Assert
        Assert.AreEqual(mockViewModel.Object, result);
    }

    [TestMethod]
    public void MainViewModel_InitializesCollections()
    {
        // Arrange & Act
   var viewModel = new MainViewModel();
        
        // Assert
 Assert.IsNotNull(viewModel.InputInspectionData);
        Assert.IsNotNull(viewModel.BendingUAT1Data);
   Assert.IsNotNull(viewModel.BendingIndicators);
        Assert.IsTrue(viewModel.InputInspectionData.Count > 0);
    }
}
```

---

## Quick Comparison Table

| Approach | Testability | Simplicity | Flexibility |
|----------|-------------|-----------|------------|
| **XAML DataContext** | ? Poor | ? High | ?? Medium |
| **Factory Pattern** | ?? Medium | ? High | ?? Medium |
| **Dependency Injection** | ? Excellent | ?? Medium | ? Excellent |
| **Data Template** | ?? Medium | ? High | ?? Medium |

---

## Conclusion

**For your refactored application, use Dependency Injection (Solution 3)** because:

1. ? Fully supports MVVM pattern
2. ? Highly testable with mocks
3. ? Industry best practice
4. ? Scales well with application growth
5. ? Future-proof for .NET upgrades

This approach aligns with modern .NET practices and makes your application maintainable and testable.

