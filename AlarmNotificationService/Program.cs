using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace AlarmNotificationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    AlarmConfig alarmConfig = configuration.GetSection("Alarm").Get<AlarmConfig>();
                    RedisConfig redisConfig = configuration.GetSection("Redis").Get<RedisConfig>();

                    services.AddSingleton(alarmConfig);
                    services.AddSingleton(redisConfig);
                    services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(String.Format("{0}:{1}, password={2}",redisConfig.ip,redisConfig.port,redisConfig.password) ));
                   
                    // services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(alarmConfig.RedisConnection));
                    services.AddHostedService<Worker>();
                });
    }
}
