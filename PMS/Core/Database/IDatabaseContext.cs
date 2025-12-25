using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using PMS.Core.Models;

namespace PMS.Core.Database;

public interface IDatabaseContext
{
    IMongoDatabase Database { get; }
    
    IMongoCollection<T> GetCollection<T>(string name);


    Task<T?> FindOneAsync<T>(Expression<Func<T, bool>> filter, string? collectionName = null)
        where T : BaseEntity;

}