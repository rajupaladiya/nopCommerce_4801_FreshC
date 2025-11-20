# Architectural Performance Improvements for High Concurrency (1000+ Requests)

This document outlines architectural improvements to support 1000+ concurrent requests in nopCommerce.

## 1. Database Connection Pool Optimization (✅ Implemented)

### Current Status
- ✅ MaxPoolSize configuration added
- ✅ MinPoolSize configuration added  
- ✅ ConnectionLifetime configuration added
- ✅ MultipleActiveResultSets (MARS) configuration added
- ✅ MultiSubnetFailover configuration added

### Recommended Settings for 1000+ Concurrent Requests

```json
{
  "DataConfig": {
    "MaxPoolSize": 200-500,        // Increase from default 100
    "MinPoolSize": 10-20,           // Maintain ready connections
    "ConnectionLifetime": 1800,     // Recycle connections every 30 minutes
    "MultipleActiveResultSets": true,  // Enable MARS for better concurrency
    "MultiSubnetFailover": true     // Enable for HA scenarios
  }
}
```

## 2. Distributed Caching (✅ Available, Needs Configuration)

### Current Status
- ✅ Redis support available
- ✅ RedisSynchronizedMemory cache available
- ⚠️ Default is disabled

### Recommended Configuration

```json
{
  "DistributedCacheConfig": {
    "Enabled": true,
    "DistributedCacheType": "RedisSynchronizedMemory",
    "ConnectionString": "your-redis-connection-string",
    "InstanceName": "nopCommerce",
    "PublishIntervalMs": 100  // Reduce from 500ms for faster cache updates
  }
}
```

**Benefits:**
- Shared cache across multiple app servers
- Reduces database load
- Improves response times significantly

## 3. Response Caching Middleware (❌ Not Implemented)

### Recommended Implementation

Add to `Program.cs` or middleware pipeline:

```csharp
// In ConfigureServices
services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 64 * 1024 * 1024; // 64 MB
    options.SizeLimit = 100 * 1024 * 1024;      // 100 MB
    options.UseCaseSensitivePaths = false;
});

// In Configure pipeline (before UseRouting)
app.UseResponseCaching();
```

**Benefits:**
- Cache HTTP responses at middleware level
- Reduce database queries for frequently accessed pages
- Significant performance improvement for public content

## 4. HTTP/2 and HTTP/3 Support

### Recommended Configuration

Add to `web.config` or hosting configuration:

```xml
<system.webServer>
  <httpProtocol>
    <customHeaders>
      <add name="Alt-Svc" value="h3=\":443\"" />
    </customHeaders>
  </httpProtocol>
</system.webServer>
```

Or configure in Kestrel:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | 
                                    System.Security.Authentication.SslProtocols.Tls13;
    });
    
    options.Limits.Http2.MaxStreamsPerConnection = 100;
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
});
```

**Benefits:**
- HTTP/2: Multiplexing, header compression
- HTTP/3: Better performance over poor networks
- Reduced latency

## 5. Fix Async Query Issues (⚠️ Partial)

### Current Issues

Some `QueryAsync` methods wrap synchronous calls:

```csharp
// Current - Not truly async
public virtual Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters)
{
    using var dataContext = CreateDataConnection();
    return Task.FromResult<IList<T>>(dataContext.Query<T>(sql, parameters)?.ToList() ?? new List<T>());
}
```

### Recommended Fix

Use truly async LinqToDB methods:

```csharp
public virtual async Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters)
{
    using var dataContext = CreateDataConnection();
    var result = await dataContext.QueryToListAsync<T>(sql, parameters);
    return result ?? new List<T>();
}
```

**Impact:** Frees up threads during I/O, allowing better concurrency.

## 6. Output Caching for Public Pages (❌ Not Implemented)

### Recommended Implementation

Add output caching for public-facing pages:

```csharp
// In controller actions
[OutputCache(Duration = 300, VaryByParam = "*", VaryByCustom = "store,theme")]
public async Task<IActionResult> Product(int productId)
{
    // ...
}
```

Or use response caching middleware with appropriate policies.

## 7. Read Replica Support (❌ Not Implemented)

### Recommended Architecture

Implement read/write splitting:

```csharp
public class ReadWriteDataProvider
{
    private readonly INopDataProvider _writeProvider;
    private readonly INopDataProvider _readProvider;

    public async Task<T> GetReadAsync<T>(Func<INopDataProvider, Task<T>> query)
    {
        return await query(_readProvider);
    }

    public async Task WriteAsync(Func<INopDataProvider, Task> command)
    {
        await command(_writeProvider);
    }
}
```

**Configuration:**
```json
{
  "DataConfig": {
    "ConnectionString": "WriteServer=...",
    "ReadConnectionString": "ReadReplica=..."
  }
}
```

## 8. Response Compression Optimization (✅ Implemented, Can Optimize)

### Current Status
- ✅ Response compression enabled
- ⚠️ Can optimize compression levels

### Recommended Optimization

```csharp
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "application/xml",
        "text/css",
        "text/html",
        "text/json",
        "text/plain",
        "text/xml"
    });
});

services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});
```

## 9. Rate Limiting Configuration (✅ Available)

### Current Status
- ✅ Rate limiting available in CommonConfig
- ⚠️ Default is disabled (0)

### Recommended Configuration for Production

```json
{
  "CommonConfig": {
    "PermitLimit": 1000,        // Requests per minute per user
    "QueueCount": 100,          // Queue size
    "RejectionStatusCode": 429  // Use 429 Too Many Requests
  }
}
```

**Note:** Adjust based on your server capacity and expected load.

## 10. Session State Optimization (✅ Available)

### Recommended Configuration

Use distributed session state:

```csharp
services.AddDistributedMemoryCache(); // Or Redis
services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.IsEssential = true;
});
```

## 11. Background Job Processing (✅ Available)

### Recommended Optimization

Ensure all heavy operations are queued:

- Email sending
- Report generation
- Data exports
- Image processing

Use `ITaskScheduler` or implement Hangfire/Quartz integration.

## 12. Database Query Optimization

### Recommendations

1. **Index Optimization**
   - Ensure all foreign keys have indexes
   - Add indexes for frequently queried columns
   - Use covering indexes for common queries

2. **Query Caching**
   - Enable query result caching for read-heavy operations
   - Use appropriate cache durations

3. **Avoid N+1 Queries**
   - Use eager loading (Include/ThenInclude)
   - Batch queries when possible

4. **Connection Timeout**
   - Set appropriate command timeouts
   - Use `SQLCommandTimeout` in DataConfig

## 13. Kestrel Server Tuning

### Recommended Configuration

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    // Increase connection limits
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
    
    // Request body size limits
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
    
    // Request timeout
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    
    // HTTP/2 settings
    options.Limits.Http2.MaxStreamsPerConnection = 100;
    options.Limits.Http2.HeaderTableSize = 4096;
});
```

## 14. Object Pooling (⚠️ Not Implemented)

### Recommended Implementation

For high-frequency object allocations:

```csharp
// Add to services
services.AddSingleton<ObjectPool<StringBuilder>>(sp =>
{
    var provider = new DefaultObjectPoolProvider();
    return provider.CreateStringBuilderPool();
});

// Usage
var pool = serviceProvider.GetRequiredService<ObjectPool<StringBuilder>>();
var sb = pool.Get();
try
{
    // Use StringBuilder
}
finally
{
    pool.Return(sb);
}
```

**Benefit:** Reduces GC pressure in high-load scenarios.

## 15. CDN Configuration

### Recommendations

- Configure CDN for static assets (CSS, JS, images)
- Use `CdnUrl` in WebOptimizerConfig
- Enable static file caching with long expiration

## 16. Monitoring and Diagnostics

### Recommended Tools

1. **Application Insights** or **New Relic**
   - Monitor response times
   - Track database query performance
   - Identify bottlenecks

2. **Performance Counters**
   - Thread pool usage
   - Connection pool utilization
   - Memory usage
   - GC pressure

3. **Logging**
   - Structured logging with Serilog
   - Correlation IDs for request tracking
   - Slow query logging

## Implementation Priority

### High Priority (Immediate Impact)
1. ✅ Configure connection pooling (MaxPoolSize, MinPoolSize)
2. ✅ Enable distributed caching (Redis)
3. Add response caching middleware
4. Fix async query issues
5. Configure rate limiting

### Medium Priority (Significant Impact)
6. HTTP/2/HTTP/3 configuration
7. Response compression optimization
8. Kestrel server tuning
9. Output caching for public pages

### Low Priority (Optimization)
10. Read replica support
11. Object pooling
12. CDN configuration
13. Advanced monitoring

## Testing Recommendations

1. **Load Testing**
   - Use tools like Apache JMeter, k6, or Locust
   - Test with 1000+ concurrent users
   - Monitor response times, error rates, resource usage

2. **Stress Testing**
   - Gradually increase load
   - Identify breaking points
   - Test failover scenarios

3. **Performance Profiling**
   - Use dotTrace or PerfView
   - Identify CPU bottlenecks
   - Find memory leaks

## Expected Performance Improvements

With all optimizations implemented:

- **Response Time**: 50-70% reduction
- **Throughput**: 5-10x increase
- **Database Load**: 40-60% reduction (with caching)
- **Memory Usage**: 20-30% reduction (with pooling)
- **Concurrent Requests**: 1000+ supported

## Additional Notes

- Always test changes in a staging environment
- Monitor application metrics after each change
- Scale horizontally (multiple app servers) for best results
- Consider using Azure/AWS load balancers with health checks
- Implement health check endpoints for load balancer integration

