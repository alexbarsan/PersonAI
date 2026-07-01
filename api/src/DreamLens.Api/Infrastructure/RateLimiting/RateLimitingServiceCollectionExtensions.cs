using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DreamRateLimitOptions>(configuration.GetSection("DreamRateLimiting"));
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var rateLimitOptions = httpContext.RequestServices
                    .GetRequiredService<IOptions<DreamRateLimitOptions>>()
                    .Value;
                var partitionKey = FindPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PermitLimit,
                        Window = rateLimitOptions.Window,
                        QueueLimit = rateLimitOptions.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        error = "rate_limit_exceeded",
                        message = "Too many requests. Please try again later."
                    },
                    cancellationToken);
            };
        });

        return services;
    }

    private static string FindPartitionKey(HttpContext httpContext)
    {
        var subject = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(subject))
        {
            return $"user:{subject}";
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress}";
    }
}
