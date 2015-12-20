using System;

namespace m.DB
{
    public sealed class MySqlPoolConfig
    {
        public string Server { get; set; }
        public uint Port { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }

        public int MaxPoolSize { get; set; }

        public TimeSpan PoolTimeout { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan CommandTimeout { get; set; }

        public MySqlPoolConfig()
        {
            Port = 3306;
            MaxPoolSize = Environment.ProcessorCount;
            PoolTimeout = TimeSpan.FromSeconds(5);
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            CommandTimeout = TimeSpan.FromSeconds(5);
        }
    }
}
