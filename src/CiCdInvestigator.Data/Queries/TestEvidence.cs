namespace CiCdInvestigator.Data.Queries;

public sealed record TestEvidence(EvidenceIdentity Identity, SourceStatus SourceStatus, IReadOnlyList<TestResult> Tests);
