using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ServiceStack.Redis;
using ServiceStack.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace worker
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

            var config = new ConfigurationBuilder()
                                .SetBasePath(AppContext.BaseDirectory)
                                .AddJsonFile("appsettings.json", false, true)
                                .AddEnvironmentVariables()
                                .Build();

            var redisConnection = config["ConnectionRedis"];
            
            var name = "gabriel";

            Log.Information("Starting worker trying to connect to " + redisConnection);

            using (var consumer = new RedisClient(redisConnection))
            {
                using (var subscription = consumer.CreateSubscription())
                {
                    subscription.OnSubscribe = (channel) =>
                    {
                        Log.Information("[{0}] Subscribe to channel '{1}'.", name, channel);
                    };
                    subscription.OnUnSubscribe = (channel) =>
                    {
                        Log.Information("[{0}] Unsubscribe to channel '{1}'.", name, channel);
                    };
                    subscription.OnMessage = (channel, message) =>
                    {
                        Log.Information("[{0}] Received a new message here '{1}' from channel '{2}'.", name, message, channel);

                        using (var client = new RedisClient(redisConnection))
                        {
                            var value = Fib(int.Parse(message)).ToString();
                            Log.Information("Information calculated for '{0}' result '{1}'.", message, value);
                            client.SetEntryInHash("values", message, value);
                        }
                    }; 

                    Log.Information("Antes de suscribir");
                    subscription.SubscribeToChannels("default");
                    Log.Information("Suscrito a channel default");
                }
            }
        }

        private static int Fib(int index) {
            if(index < 2) return 1;
            return Fib(index - 1) + Fib(index - 2);
        }
    }
}
