using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DotNetNuxt.Abstractions;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DotNetNuxt.EntityFrameworkCore;

/// <summary>
/// Abstract DbContext that implements commonly-shared IDataProvider operations
/// </summary>
public abstract class StandardIdentityDbContext(DbContextOptions options) : IdentityDbContext(options), IDataProvider
{

    #region Query Builders

    IQueryable<T> IDataProvider.Get<T>()
        => base.Set<T>();

    /// <summary>
    /// Get a query of <typeparamref name="TEntity"/> entities, 
    /// including <typeparamref name="TProperty"/> navigation properties
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <param name="navigationPropertyPath"></param>
    /// <returns></returns>
    IQueryable<TEntity> IDataProvider.GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        => base.Set<TEntity>().Include(navigationPropertyPath);

    #endregion

    #region Modifiers

    void IDataProvider.Add(IModel item) 
        => base.Add(item);

    void IDataProvider.Update(object item)
        => base.Update(item);

    void IDataProvider.Remove(object item)
        => base.Remove(item);

    Task<int> IDataProvider.ClearAsync<T>()
        => throw new NotImplementedException();

    #endregion

    #region Query Runners

    Task<List<T>> IDataProvider.ToListNoTrackingAsync<T>(IQueryable<T> query)
        => query.AsNoTracking().ToListAsync();

    Task<List<T>> IDataProvider.ToListAsync<T>(IQueryable<T> query)
        => query.ToListAsync();

    Task<int> IDataProvider.CountAsync<T>(IQueryable<T> query)
        => query.CountAsync();

    Task<bool> IDataProvider.AnyAsync<T>(IQueryable<T> query)
        => query.AnyAsync();

    #endregion

    #region Bulk Operations

    Task IDataProvider.BulkInsertAsync<T>(IList<T> items)
        => throw new NotImplementedException();

    Task<int> IDataProvider.BulkDeleteAsync<T>(IQueryable<T> items)
    {
        return items.ExecuteDeleteAsync();
    }

    /// <summary>
    /// Update multiple columns at once
    /// </summary>
    /// <remarks>
    /// This implementation allows the callers to avoid the EF Core-specific
    /// `SetPropertyCalls` symbol, and instead stick to generic symbols. Then,
    /// this implementation can be specialized by derived classes.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="newValues"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    public abstract Task<int> BulkUpdateAsync<T>(IQueryable<T> items, T newValues, List<string> columns)
        where T : IModel;

    #endregion
}
