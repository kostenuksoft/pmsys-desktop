namespace PMS.Core.Models.Common;

public class DatabaseSettings
{
    public string? Host { get; set; } = "localhost";
    public int Port { get; set; } = 27017;
    public string? DatabaseName { get; set; } = "polyclinic_db";
    public bool UseAuthentication { get; set; }
    public string? Username { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
    public string? AuthDatabase { get; set; } = string.Empty;
    public int ConnectionTimeout { get; set; } = 30;
}