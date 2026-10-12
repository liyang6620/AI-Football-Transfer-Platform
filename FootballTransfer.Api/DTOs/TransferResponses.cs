namespace FootballTransfer.Api.DTOs;

public sealed record TransferSourceResponse(
    int Id,
    string Title,
    string Source,
    string Url,
    DateTime PublishedAt,
    string? Summary);

public sealed record TransferResponse(
    int Id,
    string EventKey,
    string? PlayerName,
    string? FromClub,
    string? ToClub,
    string? TransferType,
    decimal? EstimatedFee,
    string? FeeCurrency,
    double? Confidence,
    DateTime PublishedAt,
    DateTime CreatedAt,
    string? NewsTitle,
    string? NewsUrl,
    string? NewsSource,
    string? AiSummary,
    int SourceCount,
    IReadOnlyList<TransferSourceResponse> Sources);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record SourceHealthResponse(
    string Name,
    string Url,
    string Status,
    DateTime? LastAttemptAt,
    DateTime? LastSuccessAt,
    int ItemsSeenLastRun,
    int StoredReports,
    string? LastError);

public sealed record SourceStatusResponse(
    int IntervalHours,
    IReadOnlyList<SourceHealthResponse> Sources);
