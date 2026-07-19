namespace ClinicApp.Application.Options;

/// <summary>
/// Configuration options for pagination and lookup limits across the application.
/// </summary>
public class PaginationOptions
{
    /// <summary>
    /// Number of items to return per page when the caller does not specify a page size.
    /// </summary>
    public int DefaultPageSize { get; set; } = 10;

    /// <summary>
    /// Maximum allowed page size. Requests that exceed this value are rejected.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of results to return for open-ended lookups (for example, doctor search).
    /// </summary>
    public int DefaultLookupLimit { get; set; } = 20;
}
