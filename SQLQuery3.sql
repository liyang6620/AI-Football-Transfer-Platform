DELETE FROM Transfers;
DBCC CHECKIDENT ('Transfers', RESEED, 0);
UPDATE TransferNews
SET
    IsProcessed = 0,
    AiSummary = NULL,
    ExtractedPlayer = NULL,
    ExtractedClub = NULL,
    FromClub = NULL,
    ToClub = NULL,
    TransferType = NULL,
    EstimatedFee = NULL,
    Confidence = NULL;

