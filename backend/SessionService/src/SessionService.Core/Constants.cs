namespace SessionService.Core;

public static class Constants
{
    public static class ConnectionStringNames
    {
        public const string DATABASE = "SessionDb";
        public const string RABBIT_MQ = "RabbitMQ";
        public const string REDIS = "Redis";
    }

    public static class CloseReasons
    {
        public const string EXPIRED = "expired";
        public const string SERVER_OFFLINE = "server-offline";
    }
}
