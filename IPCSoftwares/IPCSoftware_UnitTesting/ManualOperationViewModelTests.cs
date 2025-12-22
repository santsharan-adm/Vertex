using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using IPCSoftware.App.ViewModels;
using IPCSoftware.App.Services;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using Xunit;

namespace IPCSoftware_UnitTesting
{
 public class ManualOperationViewModelTests
 {
 private static T GetPrivateField<T>(object obj, string fieldName)
 {
 var fi = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
 return (T)fi.GetValue(obj)!;
 }

 [Fact]
 public void Constructor_PopulatesModes_And_Groups()
 {
 var mockLogger = new Mock<IAppLogger>();
 var mockCore = new Mock<CoreClient>(MockBehavior.Loose, new object[] { null, mockLogger.Object });

 var vm = new ManualOperationViewModel(mockLogger.Object, mockCore.Object);

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
 mockCore.Setup(c => c.WriteTagAsync(It.IsAny<int>(), It.IsAny<object>())).ReturnsAsync(true);

 var vm = new ManualOperationViewModel(mockLogger.Object, mockCore.Object);

 var mode = ManualOperationMode.TrayLiftDown;
 var item = vm.Modes.First(m => m.Mode == mode);

 // Execute command
 vm.ButtonClickCommand.Execute(mode);

 // Determine expected tag id by reading private tag map
 var tagMapA = GetPrivateField<Dictionary<ManualOperationMode, int>>(vm, "_tagMapA");
 Assert.True(tagMapA.ContainsKey(mode));
 int expectedTag = tagMapA[mode];

 // Verify WriteTagAsync called with expected tag and value1
 mockCore.Verify(c => c.WriteTagAsync(expectedTag,1), Times.Once);

 // Blinking should be set (pulse type)
 Assert.True(item.IsBlinking);

 vm.Dispose();
 }

 [Fact]
 public void ButtonClick_LatchMode_StartsAndWritesTag()
 {
 var mockLogger = new Mock<IAppLogger>();
 var mockCore = new Mock<CoreClient>(MockBehavior.Loose, new object[] { null, mockLogger.Object });
 mockCore.Setup(c => c.WriteTagAsync(It.IsAny<int>(), It.IsAny<object>())).ReturnsAsync(true);

 var vm = new ManualOperationViewModel(mockLogger.Object, mockCore.Object);

 var mode = ManualOperationMode.TransportConveyorForward;
 var item = vm.Modes.First(m => m.Mode == mode);

 vm.ButtonClickCommand.Execute(mode);

 var tagMapA = GetPrivateField<Dictionary<ManualOperationMode, int>>(vm, "_tagMapA");
 int expectedTag = tagMapA[mode];

 mockCore.Verify(c => c.WriteTagAsync(expectedTag,1), Times.Once);

 Assert.True(item.IsBlinking);

 vm.Dispose();
 }

 [Fact]
 public void ButtonClick_Stop_PerformsStopLogic_WritesExpectedTags()
 {
 var mockLogger = new Mock<IAppLogger>();
 var mockCore = new Mock<CoreClient>(MockBehavior.Loose, new object[] { null, mockLogger.Object });
 mockCore.Setup(c => c.WriteTagAsync(It.IsAny<int>(), It.IsAny<object>())).ReturnsAsync(true);

 var vm = new ManualOperationViewModel(mockLogger.Object, mockCore.Object);

 var stopMode = ManualOperationMode.TransportConveyorStop;
 vm.ButtonClickCommand.Execute(stopMode);

 var tagMapA = GetPrivateField<Dictionary<ManualOperationMode, int>>(vm, "_tagMapA");

 int fwd = tagMapA[ManualOperationMode.TransportConveyorForward];
 int rev = tagMapA[ManualOperationMode.TransportConveyorReverse];
 int low = tagMapA[ManualOperationMode.TransportConveyorLowSpeed];
 int high = tagMapA[ManualOperationMode.TransportConveyorHighSpeed];
 int stop = tagMapA[ManualOperationMode.TransportConveyorStop];

 // Motion and speed tags should be written0 at least once
 mockCore.Verify(c => c.WriteTagAsync(fwd,0), Times.AtLeastOnce);
 mockCore.Verify(c => c.WriteTagAsync(rev,0), Times.AtLeastOnce);
 mockCore.Verify(c => c.WriteTagAsync(low,0), Times.AtLeastOnce);
 mockCore.Verify(c => c.WriteTagAsync(high,0), Times.AtLeastOnce);

 // Stop tag should be pulsed with1
 mockCore.Verify(c => c.WriteTagAsync(stop,1), Times.AtLeastOnce);

 vm.Dispose();
 }

 [Fact]
 public void ButtonClick_Interlock_PreventsConflictingStart()
 {
 var mockLogger = new Mock<IAppLogger>();
 var mockCore = new Mock<CoreClient>(MockBehavior.Loose, new object[] { null, mockLogger.Object });
 mockCore.Setup(c => c.WriteTagAsync(It.IsAny<int>(), It.IsAny<object>())).ReturnsAsync(true);

 var vm = new ManualOperationViewModel(mockLogger.Object, mockCore.Object );

 var fwd = vm.Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorForward);
 var rev = vm.Modes.First(m => m.Mode == ManualOperationMode.TransportConveyorReverse);

 // Simulate forward is active
 fwd.IsActive = true;

 // Attempt to start reverse
 vm.ButtonClickCommand.Execute(ManualOperationMode.TransportConveyorReverse);

 // Verify WriteTagAsync was NOT called for reverse
 var tagMapA = GetPrivateField<Dictionary<ManualOperationMode, int>>(vm, "_tagMapA");
 int revTag = tagMapA[ManualOperationMode.TransportConveyorReverse];
 mockCore.Verify(c => c.WriteTagAsync(revTag, It.IsAny<object>()), Times.Never);

 vm.Dispose();
 }

 [Fact]
 public async Task FeedbackLoop_PulsedMode_CompletesAndResetsA()
 {
 var mockLogger = new Mock<IAppLogger>();
 var mockCore = new Mock<CoreClient>(MockBehavior.Loose, new object[] { null, mockLogger.Object });

 // Prepare GetIoValuesAsync to return B tag true
 var liveData = new Dictionary<int, object>();

 var vm = new ManualOperationViewModel(mockLogger.Object, mockCore.Object);

 // Pick a MoveToPos mode (pulse type)
 var mode = ManualOperationMode.MoveToPos1;
 var item = vm.Modes.First(m => m.Mode == mode);

 // Read tag maps
 var tagMapA = GetPrivateField<Dictionary<ManualOperationMode, int>>(vm, "_tagMapA");
 var tagMapB = GetPrivateField<Dictionary<ManualOperationMode, int>>(vm, "_tagMapB");

 int tagA = tagMapA[mode];
 int tagB = tagMapB[mode];

 // Simulate that we already sent A=1 and item is blinking
 item.IsBlinking = true;

 liveData[tagB] = true;
 mockCore.Setup(c => c.GetIoValuesAsync(It.IsAny<int>())).ReturnsAsync(liveData);
 mockCore.Setup(c => c.WriteTagAsync(It.IsAny<int>(), It.IsAny<object>())).ReturnsAsync(true);

 // Invoke the private feedback loop method via reflection
 var method = vm.GetType().GetMethod("FeedbackLoop_Tick", BindingFlags.NonPublic | BindingFlags.Instance);
 Assert.NotNull(method);

 method.Invoke(vm, new object[] { null, EventArgs.Empty });

 // After feedback, blinking should be cleared and A reset (WriteTagAsync with tagA,0)
 Assert.False(item.IsBlinking);
 mockCore.Verify(c => c.WriteTagAsync(tagA,0), Times.AtLeastOnce);

 vm.Dispose();
 }
 }
}
