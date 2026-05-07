using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Threading.RateLimiting;

#pragma warning disable IDE0007 // Use var instead of explicit type - relaxed for test code
#pragma warning disable IDE0008 // Use explicit type instead of var - relaxed for test code
#pragma warning disable IDE0053 // Use expression body for lambda - relaxed for test code
#pragma warning disable CA2000 // Dispose objects before losing scope - handled by IAsyncLifetime
namespace Homework1.Tests.Api.Endpoints;

public class RateLimitingIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            // Override the rate limiter with a test policy: 3 requests per 5 seconds
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(5),
                        PermitLimit = 3,
                        AutoReplenishment = true,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
        }));
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RateLimiter_WithinLimit_AllowsRequests()
    {
        // Act - Make 3 requests (within the 3 req/5 sec limit)
        HttpResponseMessage response1 = await _client!.GetAsync("/transactions");
        HttpResponseMessage response2 = await _client!.GetAsync("/transactions");
        HttpResponseMessage response3 = await _client!.GetAsync("/transactions");

        // Assert - All should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RateLimiter_ExceedsLimit_Returns429()
    {
        // Act - Make 4 requests (exceeds the 3 req/5 sec limit)
        HttpResponseMessage response1 = await _client!.GetAsync("/transactions");
        HttpResponseMessage response2 = await _client!.GetAsync("/transactions");
        HttpResponseMessage response3 = await _client!.GetAsync("/transactions");
        HttpResponseMessage response4 = await _client!.GetAsync("/transactions");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
        response4.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task RateLimiter_BurstRequests_ReturnsMultiple429Responses()
    {
        // Act - Make multiple requests rapidly to trigger rate limit
        List<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client!.GetAsync("/transactions"));
        }

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);

        // Assert - First 3 should be OK, rest should be 429
        int okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        int tooManyCount = responses.Count(r => r.StatusCode == (HttpStatusCode)429);

        okCount.Should().Be(3);
        tooManyCount.Should().BeGreaterThanOrEqualTo(1);
    }
}
