using System.Threading.RateLimiting;

namespace Homework1.Api.RateLimiting;

internal static class RateLimitingPolicies
{
    internal static void ConfigureRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 100,
                    AutoReplenishment = true,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }

    internal static void UseGlobalRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
    }
}
