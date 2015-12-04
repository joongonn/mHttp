using System;
using System.Linq;
using System.Threading.Tasks;

using NLog;

using m.Config;
using m.DB;
using m.Deploy;
using m.Http;

using Dapper;

namespace m.Sample
{
    public class Account
    {
        public string id { get; set; }
        public string name { get; set; }
        public string password { get; set; }

        public class CreateRequest
        {
            public string name { get; set; }
            public string password { get; set; }
        }
    }

    class SampleService
    {
        readonly MySqlPool db;

        public SampleService(MySqlPool db)
        {
            this.db = db;
        }

        public async Task<Account> CreateAccount(Account.CreateRequest create)
        {
            using (var pooled = await db.GetAsync())
            {
                await pooled.Resource.ExecuteAsync(@"INSERT INTO `account` (`name`, `password`) VALUES (@name, @password)", create);
                var ids = await pooled.Resource.QueryAsync<ulong>(@"SELECT LAST_INSERT_ID()");

                return new Account
                {
                    id = ids.Single().ToString(),
                    name = create.name,
                    password = create.password
                };
            }
        }

        public async Task<HttpResponse> GetAccountByIdEndpoint(Request req)
        {
            var id = Convert.ToInt64(req.UrlVariables["id"]);

            using (var pooled = await db.GetAsync())
            {
                var accounts = await pooled.Resource.QueryAsync<Account>("SELECT * FROM `account` WHERE `id`=@id", new { id = id });

                return new JsonResponse(accounts.Single());
            }
        }
    }

    class Program
    {
        [DeployConfigLabel("ServerConfiguration")]
        class ServerConfig : IConfigurable
        {
            [EnvironmentVariable("httpListenAddress")]
            public string ListenAddress { get; set; }

            [EnvironmentVariable("httpListenPort")]
            public int ListenPort { get; set; }

            [EnvironmentVariable("mySqlServer")]
            public string MySqlServer { get; set; }

            [EnvironmentVariable("mySqlPort")]
            public uint MySqlPort { get; set; }

            [EnvironmentVariable("mySqlDatabase")]
            public string MySqlDatabase { get; set; }

            [EnvironmentVariable("mySqlUserId")]
            public string MySqlUserId { get; set; }

            [EnvironmentVariable("mySqlPassword")]
            public string MySqlPassword { get; set; }


            public ServerConfig()
            {
                // Defaults
                ListenAddress = "*";
                ListenPort = 8080;

                MySqlServer = "localhost";
                MySqlPort = 3306;
                MySqlDatabase = "test";
                MySqlUserId = "root";
                MySqlPassword = null;
            }
        }

        public static void Main(string[] args)
        {
            LogManager.Configuration = LoggingDefaults.ToConsole(LogLevel.Debug);

            var config = ConfigManager.Load<ServerConfig>();

            var mySqlPoolConfig = new MySqlPoolConfig
            {
                Server = config.MySqlServer,
                Port = config.MySqlPort,
                Database = config.MySqlDatabase, 
                UserId = config.MySqlUserId,
                Password = config.MySqlPassword

            };
            var services = new SampleService(new MySqlPool("Sample", mySqlPoolConfig));

            var server = new HttpListenerBackend(config.ListenAddress, config.ListenPort);
            // var server = new TcpListenerBackend(System.Net.IPAddress.Any, config.ListenPort);

            var routeTable = new RouteTable(
                Route.Get("/").With((request) => new TextResponse("Hello " + request.Headers["User-Agent"])),
                Route.Post("/accounts").WithAsync(Lift.ToAsyncJsonHandler<Account.CreateRequest, Account>(services.CreateAccount)),
                Route.Get("/accounts/{id}").WithAsync(services.GetAccountByIdEndpoint),
                Route.Get("/metrics").With(server.GetMetricsReport).LimitRate(1),
                Route.Get("/export").With(Lift.ToJsonHandler(DeploymentHelper.ExportEnvironmentVariables)),
                Route.Post("/shutdown").WithAction(server.Shutdown)
            );

            var router = new Router(routeTable);

            server.Start(router);
        }
    }
}
