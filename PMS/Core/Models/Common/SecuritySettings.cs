namespace PMS.Core.Models.Common;

public static class SecuritySettings
{
    public static int BCryptWorkFactor { get; set; } = 13;
    public static int PasswordMinLength { get; set; } = 8;
    public static bool PasswordRequireUppercase { get; set; } = true;
    public static bool PasswordRequireLowercase { get; set; } = true;
    public static bool PasswordRequireDigit { get; set; } = true;
    public static bool PasswordRequireSpecialChar { get; set; } = true;
    public static int MaxFailedLoginAttempts { get; set; } = 5;
    public static int RememberMeDuration { get; set; } = 30;
}