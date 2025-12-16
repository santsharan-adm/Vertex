using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IMessagePublisher
    {
        // Method to send any message object (e.g., to a UI client or message broker)
        Task PublishAsync<T>(T message);
    }
}
