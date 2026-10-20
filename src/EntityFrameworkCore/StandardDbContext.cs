using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DotNetNuxt.Abstractions;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace DotNetNuxt.EntityFrameworkCore;

/// <summary>
/// Abstract DbContext that implements commonly-shared IDataProvider operations
/// </summary>
public abstract class StandardDbContext(DbContextOptions options) : DbContext(options), IDataProvider
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

    void IDataProvider.AddRange(IEnumerable<IModel> items)
        => base.AddRange(items);

    void IDataProvider.Update(IModel item)
        => base.Update(item);

    void IDataProvider.UpdateRange(IEnumerable<IModel> items)
        => base.UpdateRange(items);

    void IDataProvider.Remove(IModel item)
        => base.Remove(item);

    void IDataProvider.RemoveRange(IEnumerable<IModel> items)
        => base.RemoveRange(items);

    Task<int> IDataProvider.ClearAsync<T>()
        => base.Set<T>().ExecuteDeleteAsync();

    #endregion

    #region Query Runners

    Task<List<T>> IDataProvider.ToListNoTrackingAsync<T>(IQueryable<T> query)
        => query.AsNoTracking().ToListAsync();

    Task<List<T>> IDataProvider.ToListAsync<T>(IQueryable<T> query)
        => query.ToListAsync();

    Task<T?> IDataProvider.SingleOrDefaultAsync<T>(IQueryable<T> query) where T : class
        => query.SingleOrDefaultAsync();

    Task<int> IDataProvider.CountAsync<T>(IQueryable<T> query)
        => query.CountAsync();

    Task<bool> IDataProvider.AnyAsync<T>(IQueryable<T> query)
        => query.AnyAsync();

    #endregion

    #region Bulk Operations

    Task IDataProvider.BulkInsertAsync<T>(IList<T> items)
        => throw new NotImplementedException();

    Task<int> IDataProvider.BulkDeleteAsync<T>(IQueryable<T> query)
        => query.ExecuteDeleteAsync();

    Task<int> IDataProvider.BulkUpdateAsync<TEntity, TProperty>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, TProperty>> propertySelector,
        TProperty newValue,
        CancellationToken cancellationToken)
    {
        return query.ExecuteUpdateAsync(
            setters => setters.SetProperty(propertySelector, newValue),
            cancellationToken);
    }

    #endregion
}
