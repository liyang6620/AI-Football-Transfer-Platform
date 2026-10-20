namespace FootballTransfer.Api.Services;

public static class TransferExtractionSchema
{
    public const string Json = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": [
            "player",
            "club",
            "fromClub",
            "toClub",
            "transferType",
            "isLoan",
            "contractStatus",
            "estimatedFee",
            "feeCurrency",
            "feeEvidence",
            "confidence",
            "summary"
          ],
          "properties": {
            "player": { "type": ["string", "null"] },
            "club": { "type": ["string", "null"] },
            "fromClub": { "type": ["string", "null"] },
            "toClub": { "type": ["string", "null"] },
            "transferType": {
              "type": "string",
              "enum": ["Rumour", "Completed Transfer", "Free Transfer", "Contract", "Unknown"]
            },
            "isLoan": { "type": "boolean" },
            "contractStatus": {
              "type": ["string", "null"],
              "enum": ["signed", "agreed", "talks", null]
            },
            "estimatedFee": { "type": ["number", "null"] },
            "feeCurrency": {
              "type": ["string", "null"],
              "enum": ["GBP", "EUR", "USD", null]
            },
            "feeEvidence": { "type": ["string", "null"] },
            "confidence": { "type": "number" },
            "summary": { "type": "string" }
          }
        }
        """;
}
