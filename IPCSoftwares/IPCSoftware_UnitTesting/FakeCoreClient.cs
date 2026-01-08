using IPCSoftware.App.Services;
using IPCSoftware.App.Services.UI;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FakeCoreClient : CoreClient
{
    public List<(int Tag, object Value)> Writes { get; } = new();

    public FakeCoreClient(UiTcpClient client, IAppLogger logger)
        : base(client, logger)
    {
    }

    // Remove 'override' keyword since CoreClient.WriteTagAsync is not virtual/abstract/override
    public new Task<bool> WriteTagAsync(int tagId, object value)
    {
        Writes.Add((tagId, value));
        return Task.FromResult(true);
    }

    public new Task<Dictionary<int, object>> GetIoValuesAsync(int reqId)
    {
        return Task.FromResult(new Dictionary<int, object>());
    }

    // Provide a simple acknowledgement implementation for tests
    public new Task<bool> AcknowledgeAlarmAsync(int alarmNo, string userName)
    {
        return Task.FromResult(true);
    }
}
