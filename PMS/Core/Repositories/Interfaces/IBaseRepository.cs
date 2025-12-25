using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PMS.Core.Models;

namespace PMS.Core.Repositories.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id);
    Task<T?> GetByPredicateAsync(Expression<Func<T, bool>> predicate);

    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAllOfAsync(Expression<Func<T, bool>> predicate);

    Task<T> CreateAsync(T entity);

    Task<long> UpdateByIdAsync(string id, T entity);
    Task<long> UpdateAsync(Expression<Func<T, bool>> predicate, T entity);

    Task<bool> DeleteByIdAsync(string id);
    Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate, T entity);

    Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null);
}