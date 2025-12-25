namespace PMS.Core.Models.Common;

public class DatabaseSettingsMemento
{
    public string? Host { get; init; }
    public int Port { get; init; }
    public string? DatabaseName { get; init; }
    public bool UseAuthentication { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? AuthDatabase { get; init; }
    public int ConnectionTimeout { get; init; }

    public DatabaseSettingsMemento(
        string? host,
        int port,
        string? databaseName,
        bool useAuthentication,
        string? username,
        string? password,
        string? authDatabase,
        int connectionTimeout)
    {
        Host = host;
        Port = port;
        DatabaseName = databaseName;
        UseAuthentication = useAuthentication;
        Username = username;
        Password = password;
        AuthDatabase = authDatabase;
        ConnectionTimeout = connectionTimeout;
    }
}