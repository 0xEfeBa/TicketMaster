using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure.Redis;

public sealed class RedisAuthRateLimitMiddleware(RequestDelegate next)
{
    private static readonly PathString LoginPath = "/api/v1/auth/login";
    private static readonly PathString RegisterPath = "/api/v1/auth/register";

    public async Task InvokeAsync(HttpContext context, IAuthEndpointRateLimiter limiter)
    {
        var path = context.Request.Path;
        if (path.Equals(LoginPath, StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(context.Request.Method))
        {
            var key = ClientKey(context);
            if (!await limiter.AllowLoginAsync(key, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Append("Retry-After", "60");
                await context.Response.WriteAsJsonAsync(new { error = "rate_limited" });
                return;
            }
        }
        else if (path.Equals(RegisterPath, StringComparison.OrdinalIgnoreCase)
                 && HttpMethods.IsPost(context.Request.Method))
        {
            var key = ClientKey(context);
            if (!await limiter.AllowRegisterAsync(key, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Append("Retry-After", "60");
                await context.Response.WriteAsJsonAsync(new { error = "rate_limited" });
                return;
            }
        }

        await next(context);
    }

    private static string ClientKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip))
            return ip;
        return context.Connection.Id;
    }
}
