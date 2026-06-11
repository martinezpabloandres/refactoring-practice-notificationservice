using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public static class  Constants
{
    public const int CacheExpirationMinutes = 30;
}

public class User
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DeviceToken { get; set; }
}

public interface INotificationStrategy
{
    void Send(User user, string message);
}

public class EmailNotification : INotificationStrategy
{
    private readonly ILogger _logger;

    public EmailNotification(ILogger logger)
    {
        _logger = logger;
    }

    public void Send(User user, string message)
    {
        _logger.LogInformation("Sending email to " + user.Email);
        _logger.LogInformation("Dear " + user.Name + ", " + message);
    }
}

public class SmsNotification : INotificationStrategy
{
    private readonly ILogger _logger;
    public SmsNotification(ILogger logger)
    {
        _logger = logger;
    }
    public void Send(User user, string message)
    {
        _logger.LogInformation("Sending SMS to " + user.Phone);
        _logger.LogInformation("Hi " + user.Name + "! " + message);
    }
}

public class PushNotification : INotificationStrategy
{
    private readonly ILogger _logger;
    public PushNotification(ILogger logger)
    {
        _logger = logger;
    }
    public void Send(User user, string message)
    {
        _logger.LogInformation("Sending push notification to device " + user.DeviceToken);
        _logger.LogInformation(message);
    }
}

public static class NotificationFactory
{
    public static INotificationStrategy Create(string type, ILogger logger)
    {
        return type switch
        {
            "email" => new EmailNotification(logger),
            "sms" => new SmsNotification(logger),
            "push" => new PushNotification(logger),
            _ => throw new ArgumentException($"Unknown notification type: {type}")
        };
    }
}

public interface IUserRepository
{
    User? GetUserById(string userId);
}

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public User? GetUserById(string userId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        using var cmd = new SqlCommand("SELECT * FROM users WHERE id = @userId", conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Name = reader["name"].ToString(),
                Email = reader["email"].ToString(),
                Phone = reader["phone"].ToString(),
                DeviceToken = reader["device_token"].ToString()
            };
        }

        return null;
    }
}

public interface INotificationLogsRepository
{
    void LogNotification(string userId, string type, string message);
}

public class NotificationLogsRepository : INotificationLogsRepository
{
    private readonly string _connectionString;
    public NotificationLogsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    public void LogNotification(string userId, string type, string message)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var cmd = new SqlCommand("INSERT INTO notification_logs (user_id, type, message, sent_at) VALUES (@userId, @type, @message, @sentAt)", conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.Parameters.AddWithValue("@message", message);
        cmd.Parameters.AddWithValue("@sentAt", DateTime.Now);
        cmd.ExecuteNonQuery();
    }
}

// Main Service
public interface INotificationService
{
    void SendNotification(string userId, string type, string message);
}

public class NotificationService : INotificationService
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationLogsRepository _notificationLogsRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUserRepository userRepository, INotificationLogsRepository notificationLogsRepository, IMemoryCache cache, ILogger<NotificationService> logger)
    {
        _userRepository = userRepository;
        _notificationLogsRepository = notificationLogsRepository;
        _cache = cache;
        _logger = logger;
    }

    public void SendNotification(string userId, string type, string message)
    {
        var cacheKey = $"user_{userId}";
        if (!_cache.TryGetValue(cacheKey, out User? user))
        {
            user = _userRepository.GetUserById(userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return;
            }
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(Constants.CacheExpirationMinutes));
        }

        var strategy = NotificationFactory.Create(type, _logger);

        strategy.Send(user, message);

        _notificationLogsRepository.LogNotification(userId, type, message);
        _logger.LogInformation($"Sent {type} notification to user {userId}: {message}");
    }
}