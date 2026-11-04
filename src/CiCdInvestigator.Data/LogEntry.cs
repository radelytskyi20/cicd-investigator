namespace CiCdInvestigator.Data;

public sealed class LogEntry
{
    public string JobId { get; set; } = "";
    public int Position { get; set; }
    public string Text { get; set; } = "";
}
