using IPCSoftware.App.ViewModels;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using Moq;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace IPCSoftware_UnitTesting
{
     public class AlarmConfigurationViewModelTests
         {
             private readonly Mock<IAlarmConfigurationService> _alarmServiceMock = new();
            private readonly Mock<IAppLogger> _loggerMock = new();

     public AlarmConfigurationViewModelTests()
         {
            }

 [Fact]
    public void Constructor_Initializes_Defaults()
        {
            var vm = new AlarmConfigurationViewModel(_alarmServiceMock.Object, _loggerMock.Object);

            Assert.Equal("Alarm Configuration - New", vm.Title);
            Assert.False(vm.IsEditMode);
             Assert.NotNull(vm.SaveCommand);
             Assert.NotNull(vm.CancelCommand);
            Assert.Equal(16, vm.AlarmBits.Count);
        }

 [Fact]
    public void InitializeNewAlarm_Resets_Properties()
        {
            var vm = new AlarmConfigurationViewModel(_alarmServiceMock.Object, _loggerMock.Object);
            vm.AlarmNo =99;
            vm.AlarmName = "ShouldReset";

             vm.InitializeNewAlarm();

            Assert.Equal("Alarm Configuration - New", vm.Title);
            Assert.False(vm.IsEditMode);
            Assert.Equal(0, vm.AlarmNo);
            Assert.True(string.IsNullOrEmpty(vm.AlarmName));
        }

 [Fact]
    public void LoadForEdit_Populates_ViewModel_Fields()
         {
            var model = new AlarmConfigurationModel
            {
                AlarmNo =5,
                AlarmName = "TestAlarm",
                TagNo =10,
                Name = "TagName",
                AlarmBit = "Bit3",
                AlarmText = "Some text",
                Severity = "Warning",
                AlarmTime = new DateTime(2020,1,2,3,4,5),
                AlarmResetTime = new DateTime(2021,2,3,4,5,6),
                AlarmAckTime = new DateTime(2022,3,4,5,6,7),
                Description = "Desc",
                Remark = "Rem"
             };

             var vm = new AlarmConfigurationViewModel(_alarmServiceMock.Object, _loggerMock.Object);
             vm.LoadForEdit(model);

             Assert.Equal("Alarm Configuration - Edit", vm.Title);
             Assert.True(vm.IsEditMode);
             Assert.Equal(5, vm.AlarmNo);
             Assert.Equal("TestAlarm", vm.AlarmName);
             Assert.Equal(10, vm.TagNo);
             Assert.Equal("TagName", vm.Name);
             Assert.Equal("Bit3", vm.AlarmBit);
             Assert.Equal("Some text", vm.AlarmText);
             Assert.Equal("Warning", vm.Severity);
             Assert.Equal(model.AlarmTime?.ToString("dd-MMM-yyyy HH:mm:ss"), vm.AlarmTime);
             Assert.Equal(model.AlarmResetTime?.ToString("dd-MMM-yyyy HH:mm:ss"), vm.AlarmResetTime);
            Assert.Equal(model.AlarmAckTime?.ToString("dd-MMM-yyyy HH:mm:ss"), vm.AlarmAckTime);
            Assert.Equal("Desc", vm.Description);
            Assert.Equal("Rem", vm.Remark);
            }

 [Fact]
            public async Task OnSaveAsync_Adds_New_Alarm_And_Raises_SaveCompleted()
            {
            var vm = new AlarmConfigurationViewModel(_alarmServiceMock.Object, _loggerMock.Object);

             vm.AlarmNo =1;
            vm.AlarmName = "NewAlarm";
             vm.TagNo =2;
             vm.Name = "Name";
             vm.AlarmBit = "Bit1";
            vm.AlarmText = "Text";
            vm.Severity = "Error";
            vm.AlarmTime = "01-Jan-202000:00:00";
            vm.AlarmResetTime = string.Empty;
            vm.AlarmAckTime = string.Empty;
            vm.Description = "Desc";
            vm.Remark = "Remark";

            AlarmConfigurationModel? passedModel = null;
            _alarmServiceMock.Setup(s => s.AddAlarmAsync(It.IsAny<AlarmConfigurationModel>()))
            .ReturnsAsync((AlarmConfigurationModel a) => a)
            .Callback<AlarmConfigurationModel>(m => passedModel = m);

             var raised = false;
            vm.SaveCompleted += (_, __) => raised = true;

 // Call private async method via reflection
            var method = typeof(AlarmConfigurationViewModel).GetMethod("OnSaveAsync", BindingFlags.NonPublic | BindingFlags.Instance);
             var task = (Task)method.Invoke(vm, null);
            await task;

            _alarmServiceMock.Verify(s => s.AddAlarmAsync(It.IsAny<AlarmConfigurationModel>()), Times.Once);
            Assert.NotNull(passedModel);
            Assert.Equal(1, passedModel.AlarmNo);
             Assert.Equal("NewAlarm", passedModel.AlarmName);
             Assert.True(raised);
             }

 [Fact]
             public async Task OnSaveAsync_Updates_Alarm_When_InEditMode()
            {
            var model = new AlarmConfigurationModel { AlarmNo =7, AlarmName = "Orig" };
            var vm = new AlarmConfigurationViewModel(_alarmServiceMock.Object, _loggerMock.Object);
            vm.LoadForEdit(model); // sets IsEditMode = true

            vm.AlarmName = "Edited"; // modify before save

            AlarmConfigurationModel? passedModel = null;
            _alarmServiceMock.Setup(s => s.UpdateAlarmAsync(It.IsAny<AlarmConfigurationModel>()))
            .ReturnsAsync(true)
            .Callback<AlarmConfigurationModel>(m => passedModel = m);

            var raised = false;
            vm.SaveCompleted += (_, __) => raised = true;

            var method = typeof(AlarmConfigurationViewModel).GetMethod("OnSaveAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method.Invoke(vm, null);
            await task;

            _alarmServiceMock.Verify(s => s.UpdateAlarmAsync(It.IsAny<AlarmConfigurationModel>()), Times.Once);
            Assert.NotNull(passedModel);
            Assert.Equal(7, passedModel.AlarmNo);
            Assert.Equal("Edited", passedModel.AlarmName);
            Assert.True(raised);
            }

 [Fact]
        public async Task OnSaveAsync_Logs_Error_When_Service_Throws()
            {
                var vm = new AlarmConfigurationViewModel(_alarmServiceMock.Object, _loggerMock.Object);
                vm.AlarmNo =2;
                vm.AlarmName = "WillThrow";

                _alarmServiceMock.Setup(s => s.AddAlarmAsync(It.IsAny<AlarmConfigurationModel>()))
                .ThrowsAsync(new Exception("boom"));

                var method = typeof(AlarmConfigurationViewModel).GetMethod("OnSaveAsync", BindingFlags.NonPublic | BindingFlags.Instance);
                var task = (Task)method.Invoke(vm, null);
                await task;

                _loggerMock.Verify(l => l.LogError(
                It.Is<string>(s => s.Contains("boom")),
                It.Is<LogType>(t => t == LogType.Diagnostics),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            }

 [Fact]
        public void CancelCommand_Raises_CancelRequested()
            {
            var vm = new AlarmConfigurationViewModel(_alarmServiceMock.Object, _loggerMock.Object);
            var raised = false;
            vm.CancelRequested += (_, __) => raised = true;

            vm.CancelCommand.Execute(null);

            Assert.True(raised);
            }

 [Fact]
        public void CanSave_Returns_False_For_Invalid_Data_And_True_For_Valid()
            {
            var vm = new AlarmConfigurationViewModel(_alarmServiceMock.Object, _loggerMock.Object);

             // default invalid
            var canExecuteDefault = vm.SaveCommand.CanExecute(null);
            Assert.False(canExecuteDefault);

            vm.AlarmNo =1;
            vm.AlarmName = "Name";

            var canExecuteValid = vm.SaveCommand.CanExecute(null);
            Assert.True(canExecuteValid);
            }
 }
}
