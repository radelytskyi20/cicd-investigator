namespace CiCdInvestigator.Data.Queries;

public sealed record EvidenceIdentity(string DatasetVersion, string RunId, string JobId, int Attempt);
