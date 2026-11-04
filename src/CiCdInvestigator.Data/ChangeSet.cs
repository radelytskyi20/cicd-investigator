namespace CiCdInvestigator.Data;

public sealed class ChangeSet
{
    public string RunId { get; set; } = "";
    public string? BaseCommitId { get; set; }
    public SourceStatus Status { get; set; }
    public string? Patch { get; set; }
}
