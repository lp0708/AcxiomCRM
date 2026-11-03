using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public record RoleSummary(string Role, string Description, int UserCount);

public class RolesViewModel
{
    public List<RoleSummary> Roles { get; set; } = new();
    public IReadOnlyList<ModulePermission> Matrix { get; set; } = Permissions.Matrix;
}
