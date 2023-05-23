using System;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

public class RedisConnectorHelper
{
    public static IConfiguration _staticConfig { get; set; }

    public RedisConnectorHelper(IConfiguration configuration)
    {
        _staticConfig = configuration;
    }

    static RedisConnectorHelper()
    {
        //var _redisConnection = ConnectionMultiplexer.Connect(_configuration.GetSection("Redis")["ConnectionString"]);

        RedisConnectorHelper.lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(ConfigurationHelper.config.GetSection("Redis")["ConnectionString"] ?? "localhost:6379");
        });
    }

    private static Lazy<ConnectionMultiplexer> lazyConnection;

    public static ConnectionMultiplexer Connection
    {
        get
        {
            return lazyConnection.Value;
        }
    }
}