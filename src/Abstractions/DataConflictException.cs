namespace DotNetNuxt.Abstractions;

/// <summary>
/// A concurrent data change prevented an operation from being committed.
/// </summary>
public class DataConflictException(Exception innerException)
    : Exception("The data changed during this operation. Reload and try again.", innerException);
