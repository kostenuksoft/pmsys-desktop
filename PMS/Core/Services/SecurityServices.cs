using Microsoft.Extensions.Options;
using PMS.Core.Enums.General;
using Serilog;
using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using PMS.Core.Models.Common;

namespace PMS.Core.Services
{
    public interface ISecurityService
    {
        string HashPassword(string password);
        bool VerifyPassword(string? password, string hash);
        bool ValidatePasswordStrength(string password, out string errorMessage);
        string GenerateSecureToken();
        bool IsPasswordExpired(DateTime lastPasswordChange, int? expirationDays = null);
    }

    public class SecurityService(ILogger logger) : ISecurityService
    {
        public string HashPassword(string password)
        {
            try
            {
                return BCrypt.Net.BCrypt.HashPassword(password, SecuritySettings.BCryptWorkFactor);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error hashing password");
                throw new InvalidOperationException("Failed to hash password", ex);
            }
        }

        public bool VerifyPassword(string? password, string hash)
        {
            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                    return false;

                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error verifying password");
                return false;
            }
        }

        public bool ValidatePasswordStrength(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "Пароль не може бути порожнім";
                return false;
            }

            if (password.Length < SecuritySettings.PasswordMinLength)
            {
                errorMessage = $"Пароль повинен містити мінімум {SecuritySettings.PasswordMinLength} символів";
                return false;
            }

            if (SecuritySettings.PasswordRequireUppercase && !Regex.IsMatch(password, "[A-Z]"))
            {
                errorMessage = "Пароль повинен містити хоча б одну велику літеру";
                return false;
            }

            if (SecuritySettings.PasswordRequireLowercase && !Regex.IsMatch(password, "[a-z]"))
            {
                errorMessage = "Пароль повинен містити хоча б одну малу літеру";
                return false;
            }

            if (SecuritySettings.PasswordRequireDigit && !Regex.IsMatch(password, "[0-9]"))
            {
                errorMessage = "Пароль повинен містити хоча б одну цифру";
                return false;
            }

            if (SecuritySettings.PasswordRequireSpecialChar && !Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]"))
            {
                errorMessage = "Пароль повинен містити хоча б один спеціальний символ";
                return false;
            }

            return true;
        }

        public string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var token = new char[32];
            var bytes = new byte[32];

            rng.GetBytes(bytes);

            for (int i = 0; i < token.Length; i++)
            {
                token[i] = chars[bytes[i] % chars.Length];
            }

            return new string(token);
        }

        public bool IsPasswordExpired(DateTime lastPasswordChange, int? expirationDays = null)
        {
            if (!expirationDays.HasValue)
                return false;

            var daysSinceChange = (DateTime.UtcNow - lastPasswordChange).TotalDays;
            return daysSinceChange > expirationDays.Value;
        }
    }

    public class PasswordUpdateRequest
    {
        public string? CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; }     = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool RequiresPasswordChange { get; set; }
    }

}
