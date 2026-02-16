using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.Messaging;
using Moq;
using System.Reflection;

namespace IPCSoftware_UnitTesting.ViewModelTests;

public class CoreClientTests
{
    private readonly Mock<UiTcpClient> _mockTcpClient;
    private readonly Mock<IAppLogger> _mockLogger;
    private readonly Mock<IDialogService> _dialogMock = new();
    //  private readonly Mock<IAppLogger> _mockLogger;
    //  private readonly Mock<UiTcpClient> _mockTcpClient;
    //

    public CoreClientTests()
    {
        _mockLogger = new Mock<IAppLogger>();
        _mockTcpClient = new Mock<UiTcpClient>(MockBehavior.Loose, _dialogMock.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetIoValuesAsync_ReturnsEmpty_WhenResponseNull()
    {
        var uiTcpClientFake = new UiTcpClientFake(_dialogMock.Object, _mockLogger.Object)
        {
            IsConnected = false
        };
        var client = new CoreClient(uiTcpClientFake, _mockLogger.Object);
        // Act & Assert
        var result = await client.GetIoValuesAsync(1);
        Assert.Empty(result);
    }

    [Fact]
    public async Task WriteTagAsync_ReturnsFalse_WhenDisconnected()
    {
        var uiTcpClientFake = new UiTcpClientFake(_dialogMock.Object, _mockLogger.Object)
        {
            IsConnected = false
        };

        var client = new CoreClient(uiTcpClientFake, _mockLogger.Object);

        var result = await client.WriteTagAsync(5, 123);

        Assert.False(result);
    }


    [Fact]
    public async Task AcknowledgeAlarmAsync_ReturnsFalse_OnDisconnected()
    {

        var uiTcpClientFake = new UiTcpClientFake(_dialogMock.Object, _mockLogger.Object)
        {
            IsConnected = false
        };

        var client = new CoreClient(uiTcpClientFake, _mockLogger.Object);

        var res = await client.AcknowledgeAlarmAsync(10, "user");

        Assert.False(res);
    }


    [Fact]
    public void OnDataReceived_RaisesAlarmEvent_WhenAlarmJson()
    {
        var uiTcpClientFake = new UiTcpClientFake(_dialogMock.Object, _mockLogger.Object);
        var client = new CoreClient(uiTcpClientFake, _mockLogger.Object);
        AlarmMessage received = null;
        client.OnAlarmMessageReceived += (a) => received = a;
        string json = @"{ ""AlarmInstance"": { ""AlarmNo"": 1, ""Id"": 1 }, ""MessageType"": 0 }";

        // Use reflection to invoke the private method:
        var method = typeof(CoreClient).GetMethod("OnDataReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(client, new object[] { json });

        Assert.NotNull(received);
        Assert.Equal(1, received.AlarmInstance.AlarmNo);
    }


    [Fact]
    
    public void OnDataReceived_SetsResponseTcs_WhenResponseJson()
    {
        var tcpFake = new UiTcpClientFake(_dialogMock.Object, _mockLogger.Object);
        var client = new CoreClient(tcpFake, _mockLogger.Object);

        // Prepare a TaskCompletionSource and set it into the private field _currentResponseTcs
        var tcs = new TaskCompletionSource<string>();
        var field = typeof(CoreClient).GetField("_currentResponseTcs", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field.SetValue(client, tcs);

        string responseJson = @"{""ResponseId"":1, ""Success"":true, ""Parameters"":{}}";

        // Trigger the message
        var method = typeof(CoreClient).GetMethod("OnDataReceived", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(client, new object[] { responseJson });

        Assert.True(tcs.Task.IsCompleted);
        Assert.Equal(responseJson, tcs.Task.Result);
    }
}
