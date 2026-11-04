namespace CiCdInvestigator.Data;

public sealed class SavedReport
{
    public Guid Id { get; set; }
    public DateTimeOffset SavedAt { get; set; }
    public string RunId { get; set; } = "";
    public string Goal { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string Summary { get; set; } = "";
    public string ReportJson { get; set; } = "";
}
