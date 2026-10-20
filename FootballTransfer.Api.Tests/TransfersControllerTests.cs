using System.Text.Json;
using FootballTransfer.Api.Controllers;
using FootballTransfer.Api.Data;
using FootballTransfer.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FootballTransfer.Api.Tests;

public sealed class TransfersControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetAll_returns_server_pagination_metadata()
    {
        await using var context = CreateContext();
        await SeedTransfers(context);
        var controller = new TransfersController(context);

        var result = Assert.IsType<OkObjectResult>(await controller.GetAll(page: 2, pageSize: 1));
        using var json = ToJson(result.Value);

        Assert.Equal(2, json.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, json.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, json.RootElement.GetProperty("totalPages").GetInt32());
        Assert.Single(json.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task GetAll_filters_transfer_category()
    {
        await using var context = CreateContext();
        await SeedTransfers(context);
        var controller = new TransfersController(context);

        var result = Assert.IsType<OkObjectResult>(await controller.GetAll(category: "official"));
        using var json = ToJson(result.Value);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.Single(items);
        Assert.Equal("Completed Transfer", items[0].GetProperty("transferType").GetString());
        Assert.True(items[0].GetProperty("isLoan").GetBoolean());
        Assert.False(items[0].GetProperty("isLoanFee").GetBoolean());
    }

    [Fact]
    public async Task Detail_aggregates_sources_without_exposing_article_content()
    {
        await using var context = CreateContext();
        await SeedTransfers(context);
        var controller = new TransfersController(context);
        var transferId = await context.Transfers
            .Where(transfer => transfer.TransferType == "Completed Transfer")
            .Select(transfer => transfer.Id)
            .SingleAsync();

        var result = Assert.IsType<OkObjectResult>(await controller.GetById(transferId));
        using var json = ToJson(result.Value);
        var root = json.RootElement;

        Assert.Equal(2, root.GetProperty("sourceCount").GetInt32());
        Assert.Equal(2, root.GetProperty("sources").GetArrayLength());
        Assert.False(root.TryGetProperty("content", out _));
        Assert.False(root.TryGetProperty("newsContent", out _));
        Assert.All(root.GetProperty("sources").EnumerateArray(), source =>
            Assert.False(source.TryGetProperty("content", out _)));
    }

    [Fact]
    public async Task Source_status_uses_configured_interval_and_persisted_health()
    {
        await using var context = CreateContext();
        context.SourceSyncStates.Add(new SourceSyncState
        {
            Name = "BBC Sport Football",
            Url = "https://feeds.bbci.co.uk/sport/football/rss.xml",
            LastAttemptAt = DateTime.UtcNow,
            LastSuccessAt = DateTime.UtcNow,
            ItemsSeen = 7
        });
        context.TransferNews.Add(new TransferNews
        {
            Title = "Report",
            Content = "private article text",
            Source = "BBC Sport Football",
            Url = "https://example.test/report",
            PublishedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BackgroundJobs:IntervalHours"] = "12" })
            .Build();
        var controller = new SourcesController(context, configuration);

        var result = Assert.IsType<OkObjectResult>(await controller.GetStatus());
        using var json = ToJson(result.Value);
        var sources = json.RootElement.GetProperty("sources").EnumerateArray().ToList();

        Assert.Equal(12, json.RootElement.GetProperty("intervalHours").GetInt32());
        Assert.Equal(3, sources.Count);
        Assert.Equal("healthy", sources.Single(source =>
            source.GetProperty("name").GetString() == "BBC Sport Football").GetProperty("status").GetString());
    }

    private static FootballTransferDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FootballTransferDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FootballTransferDbContext(options);
    }

    private static async Task SeedTransfers(FootballTransferDbContext context)
    {
        var officialPrimary = News("Official report", "BBC Sport Football", "official-primary");
        var officialSecondary = News("Second report", "The Guardian Football", "official-secondary");
        var rumourPrimary = News("Rumour report", "Google News Football Transfers", "rumour-primary");
        var official = Transfer("Player One", "Auckland FC", "Completed Transfer", officialPrimary, 0.94, 2);
        official.IsLoan = true;
        official.Sources.Add(officialSecondary);
        var rumour = Transfer("Player Two", "Wellington Phoenix", "Rumour", rumourPrimary, 0.76, 1);
        context.Transfers.AddRange(official, rumour);
        await context.SaveChangesAsync();
    }

    private static TransferNews News(string title, string source, string slug) => new()
    {
        Title = title,
        Content = "private article text",
        Source = source,
        Url = $"https://example.test/{slug}",
        PublishedAt = DateTime.UtcNow
    };

    private static Transfer Transfer(
        string player,
        string club,
        string type,
        TransferNews primary,
        double confidence,
        int daysAgo) => new()
    {
        PlayerName = player,
        ToClub = club,
        TransferType = type,
        Confidence = confidence,
        EventKey = $"{player}|{club}|{type}",
        TransferNews = primary,
        Sources = [primary],
        PublishedAt = DateTime.UtcNow.AddDays(-daysAgo),
        CreatedAt = DateTime.UtcNow
    };

    private static JsonDocument ToJson(object? value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(value, JsonOptions));
}
