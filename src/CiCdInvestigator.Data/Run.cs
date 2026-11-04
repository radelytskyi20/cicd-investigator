namespace CiCdInvestigator.Data;

public sealed class Run
{
    public string Id { get; set; } = "";
    public string PipelineId { get; set; } = "";
    public string DatasetVersion { get; set; } = "";
    public string Ref { get; set; } = "";
    public string CommitId { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public ExecutionState State { get; set; }
    public Outcome Outcome { get; set; }
    public string? Environment { get; set; }
    public string? Configuration { get; set; }
}
