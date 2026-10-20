using System.Linq.Expressions;

namespace DotNetNuxt.Abstractions;

/// <summary>
/// Provider of underlying object data storage for our models
/// </summary>
/// <remarks>
/// Platform- and storage-agnostic interface for all data stored in the system.
/// Note that we actively use multiple different data stores based on context
/// </remarks>
public interface IDataProvider
{
    #region Query Builders

    /// <summary>
    /// Get a query of <typeparamref name="T"/> entities
    /// </summary>
    /// <remarks>
    /// Returning a query allows us to further refine the query before executing it
    /// </remarks>
    /// <typeparam name="T">Which type of entities to get</typeparam>
    IQueryable<T> Get<T>() where T : class, IModel;

    /// <summary>
    /// Get a query of <typeparamref name="TEntity"/> entities,
    /// including <typeparamref name="TProperty"/> navigation properties
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <param name="navigationPropertyPath"></param>
    /// <returns></returns>
    IQueryable<TEntity> GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class, IModel;
    //                => base.Set<TEntity>().Include(navigationPropertyPath);

    #endregion

    #region Modifiers

    /// <summary>
    /// Add an item
    /// </summary>
    /// <param name="item">Item to add</param>
    void Add(IModel item);

    /// <summary>
    /// Add a range of items
    /// </summary>
    /// <param name="items">Items to add</param>
    void AddRange(IEnumerable<IModel> items);

    /// <summary>
    /// Update an item
    /// </summary>
    /// <param name="item">Item to update</param>
    void Update(IModel item);

    /// <summary>
    /// Update a range of items
    /// </summary>
    /// <param name="items">Items to update</param>
    void UpdateRange(IEnumerable<IModel> items);

    /// <summary>
    /// Remove an item
    /// </summary>
    /// <param name="item">Item to remove</param>
    void Remove(IModel item);

    /// <summary>
    /// Remove items
    /// </summary>
    /// <param name="items">Items to remove</param>
    void RemoveRange(IEnumerable<IModel> items);

    /// <summary>
    /// Clear all items from a particular table
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<int> ClearAsync<T>() where T : IModel;

    /// <summary>
    /// Save changes previously made
    /// </summary>
    /// <remarks>
    /// This is only needed in the case where we made changes to tracked objects and
    /// did NOT call update on them. Should be rare.
    /// </remarks>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Query Runners

    /// <summary>
    /// Execute ToList query asynchronously, with no tracking
    /// </summary>
    /// <typeparam name="T">Reference type of entities being queried</typeparam>
    /// <param name="query">Query to execute</param>
    /// <returns>List of items</returns>
    Task<List<T>> ToListNoTrackingAsync<T>(IQueryable<T> query) where T : class;
    Task<List<T>> ToListAsync<T>(IQueryable<T> query);

    /// <summary>
    /// Execute SingleOrDefault query asynchronously
    /// </summary>
    /// <typeparam name="T">Reference type of entities being queried</typeparam>
    /// <param name="query">Query to execute</param>
    /// <returns>Single item or default</returns>
    Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> query) where T : class;

    /// <summary>
    /// Execute Count query asynchronously
    /// </summary>
    /// <typeparam name="T">Type of entities being queried</typeparam>
    /// <param name="query">Query to execute</param>
    /// <returns>Number of items in <paramref name="query"/></returns>
    Task<int> CountAsync<T>(IQueryable<T> query);

    /// <summary>
    /// Execute "Any" query asynchronously
    /// </summary>
    /// <typeparam name="T">Type of entities being queried</typeparam>
    /// <param name="query">Query to execute</param>
    /// <returns>Whether there are any items in <paramref name="query"/></returns>
    Task<bool> AnyAsync<T>(IQueryable<T> query);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Insert many items en masse
    /// </summary>
    /// <remarks>
    /// This is much more efficient than doing it one at a time
    /// </remarks>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="items">Items to be inserted</param>
    Task BulkInsertAsync<T>(IList<T> items) where T : IModel;

    /// <summary>
    /// Delete many items en masse
    /// </summary>
    /// <remarks>
    /// This is much more efficient than doing it one at a time
    /// </remarks>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="items">Items to be deleted</param>
    /// <returns>Number of deleted items</returns>
    Task<int> BulkDeleteAsync<T>(IQueryable<T> items) where T : IModel;

    /// <summary>
    /// Update many items en masse for a specific property
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity being updated</typeparam>
    /// <typeparam name="TProperty">Type of the property being updated</typeparam>
    /// <param name="query">Query specifying which entities to update</param>
    /// <param name="propertySelector">Expression selecting the property to update</param>
    /// <param name="newValue">New value to set for the selected property</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Number of affected items</returns>
    Task<int> BulkUpdateAsync<TEntity, TProperty>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, TProperty>> propertySelector,
        TProperty newValue,
        CancellationToken cancellationToken) where TEntity : IModel;

    #endregion
}
