namespace DotNetNuxt.Abstractions;

/// <summary>
/// Identifies an object as a model stored in the database
/// </summary>
public interface IKeyedModel: IModel
{
    /// <summary>
    /// Public-facing unique identifier for this record
    /// </summary>
    /// <remarks>
    /// For compatibility with Sqlite, remember to generate a GUID for the Key
    /// when creating a new record, rather than relying on the database
    /// to generate it.
    ///
    /// Remember to create a unique non-clustered index on Key!
    /// </remarks>
    Guid Key { get; }
}
