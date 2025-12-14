using System.Threading.Tasks;

public interface IMessagePublisher
{
    // Method to send any message object (e.g., to a UI client or message broker)
    Task PublishAsync<T>(T message);
}

