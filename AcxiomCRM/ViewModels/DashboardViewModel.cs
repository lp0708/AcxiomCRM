namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public int PendingFollowUps { get; set; }

    public Dictionary<string, int> LeadStatusChart { get; set; } = new();
    public Dictionary<string, int> PipelineChart { get; set; } = new();
    public Dictionary<string, decimal> MonthlySalesChart { get; set; } = new();
}
