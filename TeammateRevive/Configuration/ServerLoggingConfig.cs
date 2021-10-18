namespace TeammateRevive.Configuration
{
    public class ServerLoggingConfig
    {
        public bool IsEnabled { get; set; }

        public string Url { get; set; }

        public string UserName { get; set; }

        public string RoomName { get; set; }

        public bool LogAll { get; set; }
    }
}