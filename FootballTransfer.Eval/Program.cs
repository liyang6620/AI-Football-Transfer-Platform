using System.Text.Json;
using FootballTransfer.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

if (args.Length != 2)
{
    Console.Error.WriteLine(
        "Usage: dotnet run --project FootballTransfer.Eval -- <cases.json> <candidate-results.json>");
    return 1;
}

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
{
    Console.Error.WriteLine("OPENAI_API_KEY is required to run the real-model evaluation.");
    return 2;
}

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true
};
var cases = JsonSerializer.Deserialize<List<ExtractionCase>>(
    await File.ReadAllTextAsync(args[0]),
    jsonOptions);
if (cases is null || cases.Count == 0)
{
    Console.Error.WriteLine("The evaluation case file is empty or invalid.");
    return 1;
}

var service = new OpenAiAnalysisService(NullLogger<OpenAiAnalysisService>.Instance);
var candidates = new List<CandidateResult>(cases.Count);

foreach (var item in cases)
{
    try
    {
        if (!TransferRelevanceFilter.IsTransferRelated(item.Title, item.Content))
        {
            candidates.Add(CandidateResult.Unknown(item.Id));
            continue;
        }

        var result = await service.AnalyzeNewsAsync(item.Title, item.Content);
        var toClub = ClubNameNormalizer.Normalize(result.ToClub);
        if (string.IsNullOrWhiteSpace(toClub) && !string.IsNullOrWhiteSpace(result.Club))
        {
            toClub = ClubNameNormalizer.Normalize(result.Club);
        }

        var transferType = result.TransferType ?? "Unknown";
        var isTransfer = transferType != "Unknown"
            && !string.IsNullOrWhiteSpace(result.Player)
            && !string.IsNullOrWhiteSpace(toClub)
            && result.Confidence >= 0.7;

        candidates.Add(new CandidateResult(
            item.Id,
            isTransfer,
            result.Player,
            ClubNameNormalizer.Normalize(result.FromClub),
            toClub,
            transferType,
            result.EstimatedFee,
            result.FeeCurrency,
            result.IsLoan,
            result.Confidence));
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Evaluation failed for case {item.Id}: {exception.Message}");
        return 1;
    }
}

var outputPath = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(candidates, jsonOptions));
Console.WriteLine($"Wrote {candidates.Count} candidate results to {outputPath}.");
return 0;

internal sealed record ExtractionCase(string Id, string Title, string Content);

internal sealed record CandidateResult(
    string Id,
    bool IsTransfer,
    string? Player,
    string? FromClub,
    string? ToClub,
    string TransferType,
    decimal? EstimatedFee,
    string? FeeCurrency,
    bool IsLoan,
    double Confidence)
{
    public static CandidateResult Unknown(string id) => new(
        id,
        false,
        null,
        null,
        null,
        "Unknown",
        null,
        null,
        false,
        0);
}
