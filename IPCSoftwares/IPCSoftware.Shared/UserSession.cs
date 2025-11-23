
namespace IPCSoftware.Shared
{
    public static class UserSession
    {
        public static string Username { get; set; }
        public static string Role { get; set; }
        public static bool IsLoggedIn => !string.IsNullOrEmpty(Username);

        public static event Action OnSessionChanged;

        public static void Clear()
        {
            Username = null;
            Role = null;
            OnSessionChanged?.Invoke();
        }

        public static void Set(string user, string role)
        {
            Username = user;
            Role = role;
            OnSessionChanged?.Invoke();
        }
    }

}
