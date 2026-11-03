namespace AcxiomCRM.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";
    public const string AdminOrManager = Admin + "," + Manager;

    public static readonly string[] All = { Admin, Manager, SalesExecutive };
}
