using DocumentFormat.OpenXml.Spreadsheet;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Enums.General;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Core.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IDatabaseContext context, ILogger logger)
        : base(context, "users", logger)
    {
    }

    public async Task<User?> GetByLoginAsync(string login)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Login, login);
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting user by login {Login}", login);
            throw;
        }
    }

    public async Task<User?> GetByLoginOrEmailAsync(string loginOrEmail)
    {
        try
        {
            var filterBuilder = Builders<User>.Filter;
            var filter = filterBuilder.Or(
                filterBuilder.Eq(u => u.Login, loginOrEmail),
                filterBuilder.Eq(u => u.Email, loginOrEmail)
            );

            return await Collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting user by login or email {LoginOrEmail}", loginOrEmail);
            throw;
        }
    }

    public async Task<bool> UpdatePasswordAsync(string id, string newPasswordHash)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        var update = Builders<User>.Update
            .Set(u => u.PasswordHash, newPasswordHash)
            .Set(u => u.ModifiedDate, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }


    public async Task<bool> UpdateLastLoginAsync(string userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.LastLogin, DateTime.UtcNow)
                .Set(u => u.ModifiedDate, DateTime.UtcNow);

            var result = await Collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error updating last login for user {UserId}", userId);
            throw;
        }
    }

}