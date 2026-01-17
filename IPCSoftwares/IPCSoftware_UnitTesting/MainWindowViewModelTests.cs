using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Options;
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

//namespace IPCSoftware.App.Tests
namespace IPCSoftware_UnitTesting
{
 public class MainWindowViewModelTests
 {
 private readonly Mock<INavigationService> _navMock = new();
 private readonly Mock<IDialogService> _dialogMock = new();
 private readonly Mock<IAppLogger> _loggerMock = new();
        private readonly Mock<IOptions<ExternalSettings>> _extSettingsMock = new();
        private readonly Func<ProcessSequenceWindow> _sequenceWindowFactory;
        //private readonly Mock<CoreClient> _coreClientMock = new();
        //private readonly Mock<AlarmViewModel> _alarmVmMock = new();


        private RibbonViewModel CreateVm()
        {
            return new RibbonViewModel(_extSettingsMock.Object, _navMock.Object, _dialogMock.Object, _sequenceWindowFactory, _loggerMock.Object);
        }

        [Fact]
 public void Constructor_Sets_Initial_Values()
 {
            //var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object,_loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vm = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon,alarmview, _loggerMock.Object);
            
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
          //  var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon, alarmview, _loggerMock.Object);
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
          //  var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon, alarmview, _loggerMock.Object);
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
           // var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon, alarmview, _loggerMock.Object);
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
          //  var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon, alarmview, _loggerMock.Object);
            var mainType = vmObj.GetType();

 var method = mainType.GetMethod("OnSidebarItemClick", BindingFlags.NonPublic | BindingFlags.Instance);
 method.Invoke(vmObj, new object[] { "OEE Dashboard" });

 _navMock.Verify(n => n.NavigateMain<IPCSoftware.App.Views.OEEDashboard>(), Times.Once);
 }

 [Fact]
 public void OnSidebarItemClick_Navigates_To_AuditLogs()
 {
           // var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon, alarmview, _loggerMock.Object);
            var mainType = vmObj.GetType();

 var method = mainType.GetMethod("OnSidebarItemClick", BindingFlags.NonPublic | BindingFlags.Instance);
 method.Invoke(vmObj, new object[] { "Audit Logs" });

 _navMock.Verify(n => n.NavigateToLogs(IPCSoftware.Shared.Models.ConfigModels.LogType.Audit), Times.Once);
 }

 [Fact]
 public void ExecuteCloseApp_Calls_Dialog_ShowYesNo()
 {
          //  var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vmObj = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon, alarmview, _loggerMock.Object);
            var mainType = vmObj.GetType();

 var method = mainType.GetMethod("ExecuteCloseApp", BindingFlags.NonPublic | BindingFlags.Instance);
 method.Invoke(vmObj, null);

 _dialogMock.Verify(d => d.ShowYesNo("Close the application?", "Confirm Exit"), Times.Once);
 }

 [Fact]
 public void CloseAlarmBannerCommand_Hides_Banner()
 {
           // var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vm = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon, alarmview, _loggerMock.Object);

            // Ensure it's visible
            vm.IsAlarmBannerVisible = true;
 Assert.True(vm.IsAlarmBannerVisible);

 vm.CloseAlarmBannerCommand.Execute(null);
 Assert.False(vm.IsAlarmBannerVisible);
 }

 [Fact]
 public void AcknowledgeBannerAlarmCommand_CanExecute_Based_On_State()
 {
          //  var ribbon = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var ribbon = CreateVm();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, _loggerMock.Object);
            var alarmview = new AlarmViewModel(coreClient, _loggerMock.Object);
            var vm = new MainWindowViewModel(_navMock.Object, coreClient, _dialogMock.Object, ribbon, alarmview, _loggerMock.Object);

            // Default: no active alarms and banner not visible
            vm.ActiveAlarmCount =0;
 vm.IsAlarmBannerVisible = false;
 Assert.False(vm.AcknowledgeBannerAlarmCommand.CanExecute(null));

 // When there is an active alarm AND banner visible, command should be executable
 vm.ActiveAlarmCount =1;
 vm.IsAlarmBannerVisible = true;
 Assert.True(vm.AcknowledgeBannerAlarmCommand.CanExecute(null));
 }
 }

}
