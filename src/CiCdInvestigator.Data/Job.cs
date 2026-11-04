namespace CiCdInvestigator.Data;

public sealed class Job
{
    public string Id { get; set; } = "";
    public string RunId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Attempt { get; set; }
    public ExecutionState State { get; set; }
    public Outcome Outcome { get; set; }
    public SourceStatus LogsStatus { get; set; }
    public SourceStatus TestsStatus { get; set; }
}
