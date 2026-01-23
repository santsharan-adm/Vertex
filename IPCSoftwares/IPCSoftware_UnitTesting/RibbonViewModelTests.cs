using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Views;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared;
using IPCSoftware.Shared.Models;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using Xunit;

namespace IPCSoftware_UnitTesting
{
    public class RibbonViewModelTests
    {
        private readonly Mock<INavigationService> _navMock = new();
        private readonly Mock<IDialogService> _dialogMock = new();
        private readonly Mock<IAppLogger> _loggerMock = new();
        private readonly Mock<IOptions<ExternalSettings>> _extSettingsMock = new();
        private readonly Func<ProcessSequenceWindow> _sequenceWindowFactory;


        public RibbonViewModelTests()
        {
            // Ensure a clean session for each test
            UserSession.Clear();

            var extSettings = new ExternalSettings { IsMacMiniEnabled = false };
            _extSettingsMock.Setup(s => s.Value).Returns(extSettings);

            _sequenceWindowFactory = () => null;

        }


        private RibbonViewModel CreateVm()
        {
            return new RibbonViewModel(_extSettingsMock.Object, _navMock.Object, _dialogMock.Object, _sequenceWindowFactory, _loggerMock.Object);
        }

        //private RibbonViewModel CreateVm()
        //{
        //return (RibbonViewModel)Activator.CreateInstance(typeof(RibbonViewModel), new object[] { _navMock.Object, _loggerMock.Object, _dialogMock.Object });


        //       }

        [Fact]
        public void Constructor_Initializes_Commands()
        {
            //var vm = CreateVm();
            //var vm = new RibbonViewModel(_extSettingsMock.Object,_navMock.Object,_dialogMock.Object,_sequenceWindowFactory,_loggerMock.Object);
            var vm = CreateVm();
            // string un = UserSession.Username;
            Assert.NotNull(vm.NavigateDashboardCommand);
           //Assert.NotNull(vm.NavigateSettingsCommand);
            Assert.NotNull(vm.NavigateLogsCommand);
            Assert.NotNull(vm.NavigateUserMgmtCommand);
            Assert.NotNull(vm.LogoutCommand);
            Assert.NotNull(vm.NavigateLandingPageCommand);
        }

        [Fact]
        public void OpenDashboardMenu_Invokes_ShowSidebar_With_DashboardItems() ///// Collection: ["OEE Dashboard", "Time Sync", "Alarm View"] /////
        {
            //var vm = CreateVm();
           // var vm = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vm = CreateVm();
            (string Key, List<string> Items)? captured = null;
            vm.ShowSidebar = t => captured = t;
            vm.NavigateDashboardCommand.Execute(null);
            //["Dashboard", "Control", "PLC IO", "Alarm View", "Startup Condition"]
            Assert.NotNull(captured);
            Assert.Equal("DashboardMenu", captured?.Key);
            Assert.Contains("Dashboard", captured?.Items);
            Assert.Contains("Control", captured?.Items);
            Assert.Contains("PLC IO", captured?.Items);
            Assert.Contains("Alarm View", captured?.Items);
            Assert.Contains("Startup Condition", captured?.Items);

        }  

        //[Fact]
        //public void OpenSettingsMenu_Invokes_ShowSidebar_With_SettingsItems() //// Collection: ["Mode Of Operation", "Servo Parameters", "PLC IO", "Diagnostic"]
        //{
        //    //var vm = CreateVm();
        //    //var vm = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
        //    var vm = CreateVm();
        //    (string Key, List<string> Items)? captured = null;

        //    vm.ShowSidebar = t => captured = t;

        //   // vm.NavigateSettingsCommand.Execute(null);

        //    Assert.NotNull(captured);
        //    Assert.Equal("SettingsMenu", captured?.Key);
        //    Assert.Contains("Mode Of Operation", captured?.Items);
        //    Assert.Contains("Servo Parameters", captured?.Items);
        //    Assert.Contains("Diagnostic", captured?.Items);
        //    Assert.Contains("PLC IO", captured?.Items);
        //    // Assert.Contains("Tag Control", captured?.Items);
        //   // Assert.Contains("Alarm View", captured?.Items);

        //}

        [Fact]
        public void OpenLogsMenu_Invokes_ShowSidebar_With_LogsItems()
        {
            //var vm = CreateVm();
            //var vm = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vm = CreateVm();
            (string Key, List<string> Items)? captured = null;
            vm.ShowSidebar = t => captured = t;

            vm.NavigateLogsCommand.Execute(null);

            Assert.NotNull(captured);
            Assert.Equal("LogsMenu", captured?.Key);
            Assert.Contains("Audit Logs", captured?.Items);
            //Assert.Contains("Production Logs", captured?.Items);
            Assert.Contains("Error Logs", captured?.Items);
            Assert.Contains("Diagnostics Logs", captured?.Items);
        }

        [Fact]
        public void OpenUserMgtMenu_Does_Not_Invoke_When_Not_Admin()
        {
            UserSession.Set("Rishabh", "Operator");
            //var vm = CreateVm();
            // var vm = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vm = CreateVm();
            bool called = false;
            vm.ShowSidebar = _ => called = true;

            vm.NavigateUserMgmtCommand.Execute(null);
            //Assert.False(called);
            Assert.False(called);
        }

        [Fact]
        public void OpenUserMgtMenu_Invokes_When_Admin()
        {
            UserSession.Set("alice", "Admin"); // --> Or Role="Supervisor"
            //var vm = CreateVm();
            // var vm = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vm = CreateVm();
            (string Key, List<string> Items)? captured = null;
            vm.ShowSidebar = t => captured = t;

            vm.NavigateUserMgmtCommand.Execute(null);

            Assert.NotNull(captured);
            Assert.Equal("UserMgtMenu", captured?.Key);
            Assert.Contains("Log Config", captured?.Items);
            // Assert.Contains("User Config", captured?.Items);-->> Was removed as per new changes
            //Assert.Contains("External Interface", captured?.Items); -->> Was removed as per new changes
            Assert.Contains("Device Config", captured?.Items);
            Assert.Contains("Alarm Config", captured?.Items);
            Assert.Contains("PLC TAG Config", captured?.Items);
            Assert.Contains("Shift Config", captured?.Items);
            Assert.Contains("Report Config", captured?.Items);
            Assert.Contains("Servo Parameters", captured?.Items);
            Assert.Contains("Time Sync", captured?.Items);
            Assert.Contains("Diagnostic", captured?.Items);
            Assert.Contains("MacMini Test", captured?.Items);
            Assert.Contains("AE Limit", captured?.Items);
        }

 

        [Fact]
        public void Logout_Confirmed_Performs_Logout_And_Navigates()
        {
            // Arrange
            UserSession.Set("john", "Admin");
            _dialogMock.Setup(d => d.ShowYesNo(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            // var vm = CreateVm();
            //  var vm = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vm = CreateVm();
            bool logoutCalled = false;
            vm.OnLogout = () => logoutCalled = true;

            // Act
            vm.LogoutCommand.Execute(null);

            // Assert
            _dialogMock.Verify(d => d.ShowYesNo("Are you sure you want to logout?", "Logout"), Times.Once);
            //_loggerMock.Verify(l => l.LogInfo(It.Is<string>(s => s.Contains("Logout Sucess")), It.IsAny<IPCSoftware.Shared.Models.ConfigModels.LogType>()), Times.Once);
            _navMock.Verify(n => n.ClearTop(), Times.Once);
            _navMock.Verify(n => n.NavigateMain<LoginView>(), Times.Once);
            Assert.True(logoutCalled);
            Assert.False(UserSession.IsLoggedIn);
        }

        [Fact]
        public void OpenLandingPage_Invokes_OnLandingPageRequested_And_ModeOfOperations() //-->> Was NavigateLandingPageCommand Previously
        {
            // var vm = CreateVm();
            // var vm = new RibbonViewModel(_navMock.Object, _dialogMock.Object, _loggerMock.Object);
            var vm = CreateVm();
            bool requested = false;
            vm.OnLandingPageRequested = () => requested = true;

            vm.NavigateLandingPageCommand.Execute(null);

            Assert.True(requested);
            _navMock.Verify(n => n.NavigateMain<ModeOfOperation>(), Times.Once);
        }
    }
}
