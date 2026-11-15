using Microsoft.AspNetCore.Http.Features;

namespace Society.Api;

public static class MemberRequestSecurity
{
    public static async Task LimitBody(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path;
        long? limit = path.StartsWithSegments("/api/v1/auth") || path.StartsWithSegments("/api/v1/security") || path.StartsWithSegments("/api/v1/users/me")
            ? 64 * 1024
            : path.StartsWithSegments("/api/v1/membership/my") || (HttpMethods.IsPut(context.Request.Method) && path.StartsWithSegments("/api/v1/societies") && path.Value!.Contains("/membership/members/", StringComparison.Ordinal)) ? 512 * 1024 : null;
        if (limit == null) { await next(context); return; }
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.ContentLength > limit)
        {
            await Results.Problem(statusCode: 413, title: "Request body is too large").ExecuteAsync(context);
            return;
        }
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false }) feature.MaxRequestBodySize = limit;
        try { await next(context); }
        catch (BadHttpRequestException error) when (error.StatusCode == 413 && !context.Response.HasStarted)
        {
            await Results.Problem(statusCode: 413, title: "Request body is too large").ExecuteAsync(context);
        }
    }
}
