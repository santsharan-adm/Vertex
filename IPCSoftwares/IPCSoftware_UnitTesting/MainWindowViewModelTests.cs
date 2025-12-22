using IPCSoftware.App.Services;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models.ConfigModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xunit;

namespace IPCSoftware.App.Tests
{
 public class MainWindowViewModelTests
 {
 private readonly Mock<INavigationService> _navMock = new();
 private readonly Mock<IDialogService> _dialogMock = new();
 private readonly Mock<IAppLogger> _loggerMock = new();
 private readonly Mock<CoreClient> _coreClientMock = new();
 private readonly Mock<AlarmViewModel> _alarmVmMock = new();

        //private object? CreateRibbonInstance()
        //{
        //    // Find RibbonViewModel type in loaded assemblies
        //    var ribbonType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } }).FirstOrDefault(t => t.Name == "RibbonViewModel");

        //    if (ribbonType == null) return null;

        //    // Try to find a constructor that accepts (INavigationService, IAppLogger, IDialogService)
        //    var ctor = ribbonType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 3);
        //    if (ctor != null)
        //    {
        //        return ctor.Invoke(new object[] { _navMock.Object, _loggerMock.Object, _dialogMock.Object });
        //    }
        //    // Fallback: create uninitialized object
        //    return FormatterServices.GetUninitializedObject(ribbonType);
        //}




        //private object CreateMainWindowVm(object? ribbonInstance)
        //{
        //    var mainType = AppDomain.CurrentDomain.GetAssemblies()
        //    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
        //    .FirstOrDefault(t => t.Name == "MainWindowViewModel");

        //    if (mainType == null) throw new InvalidOperationException("MainWindowViewModel type not found");

        //    // Find constructor with4 parameters (INavigationService, IAppLogger, IDialogService, RibbonViewModel)
        //    var ctor = mainType.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 4);
        //    if (ctor != null)
        //    {
        //        return ctor.Invoke(new object[] { _navMock.Object, _loggerMock.Object, _dialogMock.Object, ribbonInstance });
        //    }

        //    // Fallback to uninitialized instance
        //    return FormatterServices.GetUninitializedObject(mainType);
        //}

          [Fact]
          public void Constructor_Sets_Initial_Values()
          {
            var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vm = new MainWindowViewModel(_navMock.Object, _coreClientMock.Object, _dialogMock.Object, ribbon, _alarmVmMock.Object, _loggerMock.Object);

        // Access properties dynamically
            Assert.NotNull(vm.RibbonVM);
            Assert.NotNull(vm.CloseAppCommand);
            Assert.NotNull(vm.MinimizeAppCommand);
            Assert.NotNull(vm.SidebarItemClickCommand);
            Assert.NotNull(vm.SystemTime);
            Assert.False(string.IsNullOrWhiteSpace((string)vm.SystemTime));
          }

         [Fact]
           public void ResetLandingState_Closes_Sidebar_And_Undocks()
          {
            var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, _coreClientMock.Object, _dialogMock.Object, ribbon, _alarmVmMock.Object, _loggerMock.Object);
            //var vmObj = CreateMainWindowVm(ribbon);
            var mainType = vmObj.GetType();

                     // Set states
            var propIsSidebarOpen = mainType.GetProperty("IsSidebarOpen");
            var propIsSidebarDocked = mainType.GetProperty("IsSidebarDocked");
            propIsSidebarOpen.SetValue(vmObj, true);
            propIsSidebarDocked.SetValue(vmObj, true);

           // Invoke private ResetLandingState
           var method = mainType.GetMethod("ResetLandingState", BindingFlags.NonPublic | BindingFlags.Instance);
               method.Invoke(vmObj, null);

                Assert.False((bool)propIsSidebarOpen.GetValue(vmObj));
                Assert.False((bool)propIsSidebarDocked.GetValue(vmObj));
            }

           [Fact]
            public void LoadSidebarMenu_Adds_Items_And_Opens()
           {
            //var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object);
            //           var vmObj = CreateMainWindowVm(ribbon);
            var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, _coreClientMock.Object, _dialogMock.Object, ribbon, _alarmVmMock.Object, _loggerMock.Object);
            var mainType = vmObj.GetType();

              var items = new List<string> { "A", "B" };
              var tuple = (Key: "menu1", Items: items);

              var method = mainType.GetMethod("LoadSidebarMenu", BindingFlags.NonPublic | BindingFlags.Instance);
              method.Invoke(vmObj, new object[] { tuple });

              var sidebarItemsProp = mainType.GetProperty("SidebarItems");
              var sidebarItems = (System.Collections.ICollection)sidebarItemsProp.GetValue(vmObj);

              Assert.Equal(2, sidebarItems.Count);

              var isOpenProp = mainType.GetProperty("IsSidebarOpen");
              Assert.True((bool)isOpenProp.GetValue(vmObj));
 }

         [Fact]
         public void LoadSidebarMenu_Toggles_When_SameKey_And_NotDocked()
          {
            var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, _coreClientMock.Object, _dialogMock.Object, ribbon, _alarmVmMock.Object, _loggerMock.Object);
            var mainType = vmObj.GetType();

            var items = new List<string> { "A" };
            var tuple = (Key: "menu1", Items: items);

            var method = mainType.GetMethod("LoadSidebarMenu", BindingFlags.NonPublic | BindingFlags.Instance);

             // First call opens
             method.Invoke(vmObj, new object[] { tuple });
            var isOpenProp = mainType.GetProperty("IsSidebarOpen");
            Assert.True((bool)isOpenProp.GetValue(vmObj));

            // Second call with same key toggles (since not docked)
            method.Invoke(vmObj, new object[] { tuple });
            Assert.False((bool)isOpenProp.GetValue(vmObj));
          }

         [Fact]
         public void OnSidebarItemClick_Navigates_To_OEEDashboard()
          {
            var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, _coreClientMock.Object, _dialogMock.Object, ribbon, _alarmVmMock.Object, _loggerMock.Object);
            var mainType = vmObj.GetType();

             var method = mainType.GetMethod("OnSidebarItemClick", BindingFlags.NonPublic | BindingFlags.Instance);
             method.Invoke(vmObj, new object[] { "OEE Dashboard" });

             _navMock.Verify(n => n.NavigateMain<IPCSoftware.App.Views.OEEDashboard>(), Times.Once);
           }

        [Fact]
         public void OnSidebarItemClick_Navigates_To_AuditLogs()
           {
            var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, _coreClientMock.Object, _dialogMock.Object, ribbon, _alarmVmMock.Object, _loggerMock.Object);
            var mainType = vmObj.GetType();

            var method = mainType.GetMethod("OnSidebarItemClick", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(vmObj, new object[] { "Audit Logs" });

            _navMock.Verify(n => n.NavigateToLogs(IPCSoftware.Shared.Models.ConfigModels.LogType.Audit), Times.Once);
             }

         [Fact]
         public void ExecuteCloseApp_Calls_Dialog_ShowYesNo()
             {
               _dialogMock.Setup(d => d.ShowYesNo(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
                 var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
                  var vmObj = new MainWindowViewModel(_navMock.Object, _coreClientMock.Object, _dialogMock.Object, ribbon, _alarmVmMock.Object, _loggerMock.Object);
                 var mainType = vmObj.GetType();

                 var method = mainType.GetMethod("ExecuteCloseApp", BindingFlags.NonPublic | BindingFlags.Instance);
                     method.Invoke(vmObj, null);

                 _dialogMock.Verify(d => d.ShowYesNo("Close the application?", "Confirm Exit"), Times.Once);
             }

 //[Fact]
 //public void ExecuteMinimizeApp_Does_Not_Throw_When_No_MainWindow()
 //{
 //           var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object);
 //           var vmObj = new MainWindowViewModel(_navMock.Object, _dialogMock.Object, ribbon);
 //           var mainType = vmObj.GetType();

 //// We can't reliably set Application.Current in unit tests; ensure method doesn't throw
 //var method = mainType.GetMethod("ExecuteMinimizeApp", BindingFlags.NonPublic | BindingFlags.Instance);
 //var ex = Record.Exception(() => method.Invoke(vmObj, null));
 //Assert.NotNull(ex);
 //}
 }
}
