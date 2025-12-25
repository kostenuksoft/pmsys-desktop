using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using PMS.Core.Database;
using PMS.Core.Models;
using PMS.Core.Repositories.Interfaces;
using Serilog;

namespace PMS.Core.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> Collection;
    protected readonly ILogger Logger;
    protected readonly FilterDefinitionBuilder<T> FilterBuilder;

    protected BaseRepository(IDatabaseContext context, string collectionName, ILogger logger)
    {
        Collection = context.GetCollection<T>(collectionName);
        Logger = logger;
        FilterBuilder = Builders<T>.Filter;
    }

    public virtual async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            var filter = FilterBuilder.Eq(x => x.Id, id);
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting {Name} by id: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<T?> GetByPredicateAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await Collection.Find(predicate).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting {Name} by custom predicate", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            var filter = FilterBuilder.Empty;
            return await Collection.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting all {Name}s", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> FindAllOfAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await Collection.Find(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error finding {Name}s", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        try
        {
            entity.CreatedDate = DateTime.UtcNow;
            entity.ModifiedDate = DateTime.UtcNow;
            await Collection.InsertOneAsync(entity);
            Logger.Information("Created new {Name} with id: {EntityId}", typeof(T).Name, entity.Id);
            return entity;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error creating {Name}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<long> UpdateByIdAsync(string id, T entity)
    {
        try
        {
            entity.Id = id;
            entity.ModifiedDate = DateTime.UtcNow;

            var filter = FilterBuilder.Eq(x => x.Id, id);
            var result = await Collection.ReplaceOneAsync(filter, entity);

            if (result.ModifiedCount > 0)
            {
                Logger.Information("Updated {Name} with id: {Id}", typeof(T).Name, id);
                return result.ModifiedCount;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error updating {Name} with id: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<long> UpdateAsync(Expression<Func<T, bool>> predicate, T entity)
    {
        try
        {
            entity.ModifiedDate = DateTime.UtcNow;

            var result = await Collection.ReplaceOneAsync(predicate, entity);

            if (result.ModifiedCount > 0)
            {
                Logger.Information("Updated {Name} with id: {Id}", typeof(T).Name, entity.Id);
                return result.ModifiedCount;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error updating {Name} with id: {Id}", typeof(T).Name, entity.Id);
            throw;
        }
    }

    public virtual async Task<bool> DeleteByIdAsync(string id)
    {
        try
        {
            var filter = FilterBuilder.Eq(x => x.Id, id);
            var result = await Collection.DeleteOneAsync(filter);

            if (result.DeletedCount > 0)
            {
                Logger.Information("Deleted {Name} with id: {Id}", typeof(T).Name, id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error deleting {Name} with id: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate, T entity)
    {
        try
        {
            var result = await Collection.DeleteOneAsync(predicate);

            if (result.DeletedCount > 0)
            {
                Logger.Information("Deleted {Name} with id: {Id}", typeof(T).Name, entity.Id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error deleting {Name} with id: {Id}", typeof(T).Name, entity.Id);
            throw;
        }
    }

    public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            if (predicate == null)
            {
                var filter = FilterBuilder.Empty;
                return await Collection.CountDocumentsAsync(filter);
            }

            return await Collection.CountDocumentsAsync(predicate);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error counting {Name}s", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            var skip = (page - 1) * pageSize;

            if (predicate == null)
            {
                var filter = FilterBuilder.Empty;
                return await Collection
                    .Find(filter)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync();
            }

            return await Collection
                .Find(predicate)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting paged {Name}s", typeof(T).Name);
            throw;
        }
    }

}