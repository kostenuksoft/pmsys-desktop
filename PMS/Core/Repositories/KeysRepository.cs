using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;

public class KeysRepository : BaseRepository<Keys>, IKeysRepository
{
    public KeysRepository(IDatabaseContext context, ILogger logger)
        : base(context, "keys", logger)
    {
    }

    public async Task<Keys?> GetByLoginAsync(string login)
    {
        try
        {
            var filter = Builders<Keys>.Filter.Eq(k => k.Login, login);
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting keys by login {Login}", login);
            throw;
        }
    }

    public async Task<bool> UpdatePasswordAsync(string keysId, string passwordHash)
    {
        try
        {
            var filter = Builders<Keys>.Filter.Eq(k => k.Id, keysId);
            var update = Builders<Keys>.Update
                .Set(k => k.PasswordHash, passwordHash)
                .Set(k => k.LastPasswordChange, DateTime.UtcNow)
                .Set(k => k.ModifiedDate, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error updating password for keys {KeysId}", keysId);
            throw;
        }
    }

    public async Task<bool> UpdatePasswordWithExpiryAsync(string keysId, string passwordHash, DateTime? expiresAt)
    {
        try
        {
            var filter = Builders<Keys>.Filter.Eq(k => k.Id, keysId);

            var updateBuilder = Builders<Keys>.Update
                .Set(k => k.PasswordHash, passwordHash)
                .Set(k => k.LastPasswordChange, DateTime.UtcNow)
                .Set(k => k.FailedLoginAttempts, 0)
                .Set(k => k.ModifiedDate, DateTime.UtcNow);

            if (expiresAt.HasValue)
            {
                updateBuilder = updateBuilder.Set(k => k.PasswordExpires, expiresAt.Value);
            }

            var result = await Collection.UpdateOneAsync(filter, updateBuilder);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error updating password with expiry for keys {KeysId}", keysId);
            throw;
        }
    }

    public async Task<bool> UnlockAccountAsync(string keysId)
    {
        try
        {
            var filter = Builders<Keys>.Filter.Eq(k => k.Id, keysId);
            var update = Builders<Keys>.Update
                .Set(k => k.AccountLocked, false)
                .Set(k => k.FailedLoginAttempts, 0)
                .Set(k => k.LastFailedAttempt, null)
                .Set(k => k.ModifiedDate, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error unlocking account for keys {KeysId}", keysId);
            throw;
        }
    }

    public async Task<bool> ResetFailedAttemptsAsync(string keysId)
    {
        try
        {
            var filter = Builders<Keys>.Filter.Eq(k => k.Id, keysId);
            var update = Builders<Keys>.Update
                .Set(k => k.FailedLoginAttempts, 0)
                .Set(k => k.LastFailedAttempt, null)
                .Set(k => k.ModifiedDate, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error resetting failed attempts for keys {KeysId}", keysId);
            throw;
        }
    }

    public async Task<bool> IncrementFailedLoginAttemptsAsync(string keysId, int maxAttempts)
    {
        try
        {
            var filter = Builders<Keys>.Filter.Eq(k => k.Id, keysId);
            var keys = await Collection.Find(filter).FirstOrDefaultAsync();

            if (keys == null)
                return false;

            var newAttempts = keys.FailedLoginAttempts + 1;
            var shouldLock = newAttempts >= maxAttempts;

            var update = Builders<Keys>.Update
                .Set(k => k.FailedLoginAttempts, newAttempts)
                .Set(k => k.LastFailedAttempt, DateTime.UtcNow)
                .Set(k => k.AccountLocked, shouldLock)
                .Set(k => k.ModifiedDate, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(filter, update);

            if (shouldLock)
            {
                Logger.Warning("Account locked for keys {KeysId} after {Attempts} failed attempts",
                    keysId, newAttempts);
            }

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error incrementing failed login attempts for keys {KeysId}", keysId);
            throw;
        }
    }

    public async Task<bool> IsAccountLockedAsync(string login)
    {
        try
        {
            var filterBuilder = Builders<Keys>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(k => k.Login, login),
                filterBuilder.Eq(k => k.AccountLocked, true)
            );

            var count = await Collection.CountDocumentsAsync(filter);
            return count > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error checking if account is locked {Login}", login);
            throw;
        }
    }

    public async Task<bool> ExistsByLoginAsync(string login)
    {
        try
        {
            var filter = Builders<Keys>.Filter.Eq(k => k.Login, login);
            var count = await Collection.CountDocumentsAsync(filter);
            return count > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error checking if keys exist by login {Login}", login);
            throw;
        }
    }

    public async Task<bool> UpdateLastLoginAsync(string keysId)
    {
        try
        {
            var filter = Builders<Keys>.Filter.Eq(k => k.Id, keysId);
            var update = Builders<Keys>.Update
                .Set(k => k.ModifiedDate, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error updating last login for keys {KeysId}", keysId);
            throw;
        }
    }
}