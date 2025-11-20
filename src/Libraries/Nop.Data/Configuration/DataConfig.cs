using System.Configuration;
using FluentMigrator.Runner.Initialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nop.Core.Configuration;

namespace Nop.Data.Configuration;

public partial class DataConfig : IConfig, IConnectionStringAccessor
{
    /// <summary>
    /// Gets or sets a connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a data provider
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public DataProviderType DataProvider { get; set; } = DataProviderType.SqlServer;

    /// <summary>
    /// Gets or sets the wait time (in seconds) before terminating the attempt to execute a command and generating an error.
    /// By default, timeout isn't set and a default value for the current provider used.
    /// Set 0 to use infinite timeout.
    /// </summary>
    public int? SQLCommandTimeout { get; set; } = null;

    /// <summary>
    /// Gets or sets a value that indicates whether to add NoLock hint to SELECT statements (Reltates to SQL Server only)
    /// </summary>
    public bool WithNoLock { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of connections allowed in the connection pool for this specific connection string (Relates to SQL Server only).
    /// The default value is 100. Valid values are between 0 and 32767.
    /// For high-concurrency scenarios, consider increasing this value to allow more concurrent database connections.
    /// </summary>
    public int? MaxPoolSize { get; set; } = null;

    /// <summary>
    /// Gets or sets the minimum number of connections to be maintained in the connection pool for this specific connection string (Relates to SQL Server only).
    /// The default value is 0. Setting a minimum pool size (e.g., 5-10) can improve performance by maintaining ready connections.
    /// </summary>
    public int? MinPoolSize { get; set; } = null;

    /// <summary>
    /// Gets or sets the maximum time (in seconds) a connection object can live in the pool before being destroyed (Relates to SQL Server only).
    /// The default value is 0, which means connections have the maximum timeout. For high-load scenarios, consider setting this to recycle connections (e.g., 1800 seconds = 30 minutes).
    /// </summary>
    public int? ConnectionLifetime { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating whether multiple active result sets (MARS) are enabled (Relates to SQL Server only).
    /// MARS allows multiple batch operations to execute on a single connection. Can improve performance for applications with high concurrency.
    /// </summary>
    public bool? MultipleActiveResultSets { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating whether to use multi-subnet failover for high availability scenarios (Relates to SQL Server only).
    /// When true, enables faster failover to a database mirror or Always On Availability Group across subnets.
    /// </summary>
    public bool? MultiSubnetFailover { get; set; } = null;

    /// <summary>
    /// Gets a section name to load configuration
    /// </summary>
    [JsonIgnore]
    public string Name => nameof(ConfigurationManager.ConnectionStrings);

    /// <summary>
    /// Gets an order of configuration
    /// </summary>
    /// <returns>Order</returns>
    public int GetOrder() => 0; //display first
}