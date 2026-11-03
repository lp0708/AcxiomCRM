using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

/// <summary>Allowed lead status transitions. Converted is only reachable through the Convert action.</summary>
public static class LeadWorkflow
{
    private static readonly Dictionary<LeadStatus, LeadStatus[]> Transitions = new()
    {
        [LeadStatus.New] = new[] { LeadStatus.Contacted, LeadStatus.Unqualified, LeadStatus.Lost },
        [LeadStatus.Contacted] = new[] { LeadStatus.Qualified, LeadStatus.Unqualified, LeadStatus.Lost },
        [LeadStatus.Qualified] = new[] { LeadStatus.Contacted, LeadStatus.Lost },
        [LeadStatus.Unqualified] = new[] { LeadStatus.Contacted, LeadStatus.Lost },
        [LeadStatus.Converted] = Array.Empty<LeadStatus>(),
        [LeadStatus.Lost] = Array.Empty<LeadStatus>(),
    };

    /// <summary>Statuses a brand-new lead may start with.</summary>
    public static readonly LeadStatus[] InitialStatuses =
        { LeadStatus.New, LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Unqualified };

    public static bool CanMove(LeadStatus from, LeadStatus to) => from == to || Transitions[from].Contains(to);

    public static IEnumerable<LeadStatus> AllowedFrom(LeadStatus current) => Transitions[current].Prepend(current);

    public static bool IsOpen(LeadStatus status) =>
        status is not (LeadStatus.Converted or LeadStatus.Lost or LeadStatus.Unqualified);
}
