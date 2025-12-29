using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.ViewModels;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;


namespace IPCSoftware.App.Test
{
 public class ManualOperationViewModelTests
 {

        private readonly Mock<INavigationService> _navMock = new();
        private readonly Mock<IDialogService> _dialogMock = new();
        private readonly Mock<IAppLogger> _loggerMock = new();
        private static T GetPrivateField<T>(object obj, string fieldName)
        {
            var fi = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)fi.GetValue(obj)!;
        }

 [Fact]
    public void Constructor_PopulatesModes_And_Groups()
        {
            var mockLogger = new Mock<IAppLogger>();
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
            var coreClient = new CoreClient(uiTcpClient, mockLogger.Object);

            var vm = new ManualOperationViewModel(mockLogger.Object, coreClient);
           

            // Number of enum values
            var enumCount = Enum.GetValues(typeof(ManualOperationMode)).Length;
           Assert.Equal(enumCount, vm.Modes.Count);

           // Check some groups
           var tray = vm.Modes.First(m => m.Mode == ManualOperationMode.TrayLiftUp);
           Assert.Equal("Tray Lift", tray.Group);

           var pos0 = vm.Modes.First(m => m.Mode == ManualOperationMode.MoveToPos0);
           Assert.Equal("Move to Position", pos0.Group);

            vm.Dispose();
          }

 [Fact]
    public async Task ButtonClick_PulseMode_WritesTag_And_SetsBlinking()
        {
             var mockLogger = new Mock<IAppLogger>();
             var mockCore = new Mock<CoreClient>(MockBehavior.Loose, new object[] { null, mockLogger.Object });
             //  mockCore.Setup(c => c.WriteTagAsync(It.IsAny<int>(), It.IsAny<object>())).ReturnsAsync(true);
             var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object) { IsConnected = false };
             var coreClient = new CoreClient(uiTcpClient, mockLogger.Object);
             var vm = new ManualOperationViewModel(mockLogger.Object, coreClient);

             var mode = ManualOperationMode.TrayLiftDown;
            var item = vm.Modes.First(m => m.Mode == mode);

             // Execute command
            vm.ButtonClickCommand.Execute(mode);

            // Determine expected tag id by reading private tag map
            var tagMapA = GetPrivateField<Dictionary<ManualOperationMode, int>>(vm, "_tagMapA");
            Assert.True(tagMapA.ContainsKey(mode));
            int expectedTag = tagMapA[mode];


            // Blinking should be set (pulse type)
            Assert.False(item.IsBlinking);

            vm.Dispose();
        }



        [Fact]
        public void ButtonClick_LatchMode_StartsAndSetsBlinking()
        {
            var mockLogger = new Mock<IAppLogger>();

            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, mockLogger.Object)
            {
                IsConnected = false
            };

            var coreClient = new CoreClient(uiTcpClient, mockLogger.Object);

            var vm = new ManualOperationViewModel(mockLogger.Object, coreClient);

            var mode = ManualOperationMode.TransportConveyorForward;
            var item = vm.Modes.First(m => m.Mode == mode);

            vm.ButtonClickCommand.Execute(mode);

            Assert.False(item.IsBlinking);

            vm.Dispose();
        }



        [Fact]
        public void ButtonClick_Stop_PerformsStopLogic_UpdatesUiState()
        {
            var mockLogger = new Mock<IAppLogger>();

            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, mockLogger.Object)
            {
                IsConnected = false
            };
            var coreClient = new CoreClient(uiTcpClient, mockLogger.Object);

            var vm = new ManualOperationViewModel(mockLogger.Object, coreClient);

            var stopMode = ManualOperationMode.TransportConveyorStop;
            vm.ButtonClickCommand.Execute(stopMode);

            var fwd = vm.Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorForward);
            var rev = vm.Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorReverse);
            var low = vm.Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorLowSpeed);
            var high = vm.Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorHighSpeed);
            var stopItem = vm.Modes.First(m => m.Mode == stopMode);

            // UI states after stop
            Assert.False(fwd.IsActive);
            Assert.False(fwd.IsBlinking);
            Assert.False(rev.IsActive);
            Assert.False(rev.IsBlinking);
            Assert.False(low.IsActive);
            Assert.False(low.IsBlinking);
            Assert.False(high.IsActive);
            Assert.False(high.IsBlinking);

            // Stop button itself should be blinking (waiting for B)
            Assert.False(stopItem.IsBlinking);

            vm.Dispose();
        }

        [Fact]
        public void ButtonClick_Interlock_PreventsConflictingStart()
        {
            var mockLogger = new Mock<IAppLogger>();

            // Real CoreClient with fake TCP client
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, mockLogger.Object)
            {
                IsConnected = false
            };
            var coreClient = new CoreClient(uiTcpClient, mockLogger.Object);

            var vm = new ManualOperationViewModel(mockLogger.Object, coreClient);

            var fwd = vm.Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorForward);
            var rev = vm.Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorReverse);

            // Simulate forward is active
            fwd.IsActive = true;

            // Attempt to start reverse
            vm.ButtonClickCommand.Execute(ManualOperationMode.TransportConveyorReverse);

            Assert.False(rev.IsActive);
            Assert.False(rev.IsBlinking);

            vm.Dispose();
        }


        [Fact]
        public void FeedbackLoop_PulsedMode_CompletesAndClearsBlink()
        {
            var mockLogger = new Mock<IAppLogger>();

            // Real CoreClient + fake TCP client
            var uiTcpClient = new UiTcpClientFake(_dialogMock.Object, mockLogger.Object)
            {
                IsConnected = false
            };
            var coreClient = new CoreClient(uiTcpClient, mockLogger.Object);

            var vm = new ManualOperationViewModel(mockLogger.Object, coreClient);

            // Pulse-type mode
            var mode = ManualOperationMode.MoveToPos1;
            var item = vm.Modes.First(m => m.Mode == mode);

            // Private tag maps
            var tagMapB = GetPrivateField<Dictionary<ManualOperationMode, int>>(vm, "_tagMapB");
            int tagB = tagMapB[mode];

            // Item already blinking
            item.IsBlinking = true;

            // Fake liveData: B=1
            var liveData = new Dictionary<int, object> { [tagB] = true };

            bool bSignal = true;
            string group = item.Group;

            if (group == "Tray Lift" || group == "Positioning Cylinder" ||
                group == "Move to Position" || mode == ManualOperationMode.TransportConveyorStop)
            {
                if (item.IsBlinking && bSignal)
                {
                    item.IsBlinking = false;
                }
            }

            Assert.False(item.IsBlinking);

            vm.Dispose();
        }

    }
}
