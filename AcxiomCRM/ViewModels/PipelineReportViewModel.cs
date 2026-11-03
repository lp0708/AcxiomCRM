namespace AcxiomCRM.ViewModels;

public record PipelineRow(string Key, int Count, decimal Amount, decimal Weighted);

public class PipelineReportViewModel
{
    public List<PipelineRow> ByStage { get; set; } = new();
    public List<PipelineRow> ByOwner { get; set; } = new();
    public PipelineRow Total { get; set; } = new("Total", 0, 0, 0);
}
