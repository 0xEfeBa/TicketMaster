namespace Identity.Infrastructure.Redis;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    public int LoginRequestsPerMinute { get; set; } = 30;

    public int RegisterRequestsPerMinute { get; set; } = 15;

    public int UserProfileCacheSeconds { get; set; } = 120;
}
