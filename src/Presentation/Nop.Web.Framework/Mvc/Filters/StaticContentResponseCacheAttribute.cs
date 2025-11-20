using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Mvc.Filters;

/// <summary>
/// Represents an attribute that enables response caching for static content pages (blog posts, topics)
/// with support for varying by store, language, and other context-specific parameters
/// </summary>
public class StaticContentResponseCacheAttribute : ResponseCacheAttribute
{
    #region Ctor

    public StaticContentResponseCacheAttribute()
    {
        var commonConfig = Singleton<AppSettings>.Instance.Get<CommonConfig>();
        
        // Use configured duration or default to 1 hour (3600 seconds)
        Duration = commonConfig.EnableStaticContentOutputCache 
            ? commonConfig.StaticContentOutputCacheDuration 
            : 0;
        
        // Cache on client, proxy, and server
        Location = ResponseCacheLocation.Any;
        
        // Vary by query string parameters (blogPostId, topicId, etc.)
        VaryByQueryKeys = new[] { "*" };
        
        // Vary by headers to support multi-store (Host) and multi-language (Accept-Language)
        VaryByHeader = "Host,Accept-Language";
        
        // Don't vary by any route values since we use query string parameters
        VaryByRouteValues = Array.Empty<string>();
    }

    #endregion

    #region Methods

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var commonConfig = Singleton<AppSettings>.Instance.Get<CommonConfig>();
        
        // Check if caching is disabled
        if (!commonConfig.EnableStaticContentOutputCache || commonConfig.StaticContentOutputCacheDuration <= 0)
        {
            Duration = 0;
            return;
        }

        // Update duration from config (in case it changed)
        Duration = commonConfig.StaticContentOutputCacheDuration;
        
        base.OnActionExecuting(context);
    }

    #endregion
}

