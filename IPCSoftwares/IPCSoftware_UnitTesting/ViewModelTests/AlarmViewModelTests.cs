using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.App.ViewModels;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Shared.Models.Messaging;
using Moq;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace IPCSoftware_UnitTesting.ViewModelTests;

public class AlarmViewModelTests
{
    private readonly UiTcpClientFake _tcpFake;
    private readonly Mock<IAppLogger> _loggerMock;
    private readonly FakeCoreClient _coreClient;
    private readonly Mock<INavigationService> _navMock = new();
    private readonly Mock<IDialogService> _dialogMock = new();
   // private readonly Mock<IAppLogger> _loggerMock = new();

    public AlarmViewModelTests()
    {
        _loggerMock = new Mock<IAppLogger>();
        _tcpFake = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object);
        _coreClient = new FakeCoreClient(_tcpFake, _loggerMock.Object);
    }

[Fact]
    public async Task ExecuteGlobalWrite_Pulses_Reset_Tag_And_Updates_Alarms()
         {
           var uiTcpClientFake = new UiTcpClientFake(_dialogMock.Object, _loggerMock.Object)
           {
            IsConnected = true,
            NextResponse = @"{""ResponseId"":39, ""Success"":true}" // Mock response
           };
           var coreClient = new CoreClient(uiTcpClientFake, _loggerMock.Object);
           var vm = new AlarmViewModel(_coreClient, _loggerMock.Object);

             // Add a sample alarm
            var alarm = new AlarmInstanceModel
            {
            AlarmNo =1,
            AlarmTime = DateTime.Now
            };

            vm.ActiveAlarms.Add(alarm);

             // Invoke private method via reflection
             var method = typeof(AlarmViewModel).GetMethod("ExecuteGlobalWrite", BindingFlags.NonPublic | BindingFlags.Instance);
             var task = (Task)method.Invoke(vm, [39, "Global Reset"]);
             await task;

            // Verify that the FakeCoreClient recorded writes for tag39
             Assert.Contains(_coreClient.Writes, w => w.Tag ==39 && (bool)w.Value == true);
             Assert.Contains(_coreClient.Writes, w => w.Tag ==39 && (bool)w.Value == false);

             // Alarm reset time should be set
             Assert.NotNull(vm.ActiveAlarms.First().AlarmResetTime);
            }

[Fact]
             public async Task AcknowledgeAlarmRequestAsync_Sets_AckTime_When_Success()
                {
                var vm = new AlarmViewModel(_coreClient, _loggerMock.Object);

                var alarm = new AlarmInstanceModel
                {
                AlarmNo =2,
                AlarmTime = DateTime.Now
                };

                vm.ActiveAlarms.Add(alarm);

                await vm.AcknowledgeAlarmRequestAsync(alarm);

                Assert.NotNull(alarm.AlarmAckTime);
                Assert.Equal(Environment.UserName, alarm.AcknowledgedByUser);
                }

[Fact]
                public void HandleIncomingAlarmMessage_Adds_And_Updates_And_Clears()
                    {
                     var vm = new AlarmViewModel(_coreClient, _loggerMock.Object);

                     var instance = new AlarmInstanceModel
                     {
                         AlarmNo =3,
                         AlarmTime = DateTime.Now
                         
                     };

                        var raised = new AlarmMessage { AlarmInstance = instance, MessageType = AlarmMessageType.Raised };

                        // Invoke private method via reflection
                        var method = typeof(AlarmViewModel).GetMethod("HandleIncomingAlarmMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(vm, new object[] { raised });

                        //Assert.NotNull(vm.ActiveAlarms);
                     //   Assert.Equal(instance.AlarmNo, vm.ActiveAlarms.First().AlarmNo);

                        //// Acknowledged
                        var ackInstance = new AlarmInstanceModel
                        {
                            AlarmNo = 3,
                            AlarmAckTime = DateTime.Now,
                            AcknowledgedByUser = "tester"
                        };

                        var ackMsg = new AlarmMessage { AlarmInstance = ackInstance, MessageType = AlarmMessageType.Acknowledged };
                        method.Invoke(vm, new object[] { ackMsg });

                        Assert.NotNull(vm.ActiveAlarms.First().AlarmAckTime);
                        Assert.Equal("tester", vm.ActiveAlarms.First().AcknowledgedByUser);

                        // Cleared
                        var clearInstance = new AlarmInstanceModel
                        {
                            AlarmNo = 3
                        };
                        var clearMsg = new AlarmMessage { AlarmInstance = clearInstance, MessageType = AlarmMessageType.Cleared };
                        method.Invoke(vm, new object[] { clearMsg });

                        Assert.Empty(vm.ActiveAlarms);
    }

[Fact]
            public void CanExecuteAcknowledge_Behavior()
            {
                var vm = new AlarmViewModel(_coreClient, _loggerMock.Object);

                var alarm = new AlarmInstanceModel
                {
                AlarmNo =3
                };

                Assert.True(vm.AcknowledgeCommand.CanExecute(alarm));

                alarm.AlarmAckTime = DateTime.Now;
                Assert.False(vm.AcknowledgeCommand.CanExecute(alarm));
             }
}
