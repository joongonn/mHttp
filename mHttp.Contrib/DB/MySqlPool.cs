using System;
using System.Data;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

namespace m.DB
{
    public sealed class MySqlPool : LazyPool<IDbConnection>
    {
        public static readonly TimeSpan DefaultPoolTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(5);

        readonly string connectionString;

        public MySqlPool(string label, MySqlPoolConfig config) : this(label,
                                                                      config.MaxPoolSize,
                                                                      config.PoolTimeout,
                                                                      config.Server,
                                                                      config.Port,
                                                                      config.Database,
                                                                      config.UserId,
                                                                      config.Password,
                                                                      config.ConnectionTimeout,
                                                                      config.CommandTimeout) { }

        public MySqlPool(string label,
                         int maxPoolSize,
                         TimeSpan defaultPoolTimeout,
                         string server,
                         uint port,
                         string database,
                         string userId,
                         string password,
                         TimeSpan connectionTimeout,
                         TimeSpan commandTimeout) : base(label, maxPoolSize, defaultPoolTimeout)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = server,
                Port = port,
                Database = database,
                UserID = userId,
                Password = password,
                Pooling = false,
                ConnectionTimeout = (uint)connectionTimeout.TotalSeconds,
                DefaultCommandTimeout = (uint)commandTimeout.TotalSeconds
            };

            connectionString = builder.GetConnectionString(true);
        }

        protected async override Task<IDbConnection> AcquireNewResourceAsync()
        {
            var conn = new MySqlConnection(connectionString);
            await Task.Run((Action)conn.Open).ConfigureAwait(false); // await MySqlConnection.OpenAsync() doesn't yield?

            return conn;
        }

        protected override bool IsResourceBroken(IDbConnection resource)
        {
            switch (resource.State)
            {
                case ConnectionState.Broken:
                case ConnectionState.Closed:
                    return true;

                default:
                    return false;
            }
        }
    }
}

