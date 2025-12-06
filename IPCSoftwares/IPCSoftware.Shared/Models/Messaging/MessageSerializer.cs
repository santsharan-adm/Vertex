
using Newtonsoft.Json;

namespace IPCSoftware.Shared.Models.Messaging
{
 
    public static class MessageSerializer
    {
        public static string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
