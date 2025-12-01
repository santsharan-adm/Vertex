using System.Text.Json;

namespace IPCSoftware.Shared.Models.Messaging
{
    public static class MessageSerializer
    {
        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj);
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
