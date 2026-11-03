namespace AcxiomCRM.Models;

public enum CustomerStatus { Active, Inactive }

public enum LeadStatus { New, Contacted, Qualified, Unqualified, Converted, Lost }

public enum LeadSource { Website, Referral, Campaign, ColdCall, Event, Other }

public enum LeadPriority { Low, Medium, High }

public enum OpportunityStage { Qualification, Proposal, Negotiation, Won, Lost }

public enum FollowUpType { Call, Meeting, Email }

public enum FollowUpStatus { Planned, Completed, Missed, Cancelled }

public enum ActivityType { Call, Meeting, Email, Task }

public enum ActivityStatus { Open, Completed }
