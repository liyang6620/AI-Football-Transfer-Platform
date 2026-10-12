using FootballTransfer.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FootballTransfer.Api.Tests;

public sealed class AdminApiKeyMiddlewareTests
{
    [Fact]
    public async Task Protected_route_rejects_a_missing_key()
    {
        var called = false;
        var middleware = CreateMiddleware(() => called = true);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/crawler/crawl";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task Protected_route_accepts_the_configured_key()
    {
        var called = false;
        var middleware = CreateMiddleware(() => called = true);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/crawler/crawl";
        context.Request.Headers["X-Admin-Api-Key"] = "test-admin-key";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task Public_read_route_does_not_require_a_key()
    {
        var called = false;
        var middleware = CreateMiddleware(() => called = true);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/transfers";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task News_read_route_requires_the_admin_key()
    {
        var called = false;
        var middleware = CreateMiddleware(() => called = true);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/news";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(called);
    }

    private static AdminApiKeyMiddleware CreateMiddleware(Action onNext)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AdminApiKey"] = "test-admin-key" })
            .Build();
        return new AdminApiKeyMiddleware(_ => { onNext(); return Task.CompletedTask; }, configuration);
    }
}
