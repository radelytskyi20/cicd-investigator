namespace CiCdInvestigator.Data;

public sealed class TestResult
{
    public string Id { get; set; } = "";
    public string JobId { get; set; } = "";
    public string Name { get; set; } = "";
    public Outcome Outcome { get; set; }
    public string? Message { get; set; }
}
