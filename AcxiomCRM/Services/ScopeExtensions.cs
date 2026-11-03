using System.Security.Claims;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

/// <summary>Record-level authorization: Admin and Manager see all records, everyone else only their own.</summary>
public static class ScopeExtensions
{
    public static string? UserId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static bool IsSalesOnly(this ClaimsPrincipal user) =>
        !user.IsInRole(Roles.Admin) && !user.IsInRole(Roles.Manager);

    public static IQueryable<T> VisibleTo<T>(this IQueryable<T> query, ClaimsPrincipal user) where T : class, IOwned
    {
        if (!user.IsSalesOnly()) return query;
        var userId = user.UserId();
        return query.Where(x => x.AssignedTo == userId);
    }
}
