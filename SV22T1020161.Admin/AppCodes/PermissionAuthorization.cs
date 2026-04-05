using Microsoft.AspNetCore.Authorization;
using SV22T1020161.Models.Constants;

namespace SV22T1020161.Admin.AppCodes;

/// <summary>
/// Requirement cho kiểm tra quyền: user cần có ÍT NHẤT MỘT trong danh sách permissions
/// (OR logic: permission1 OR permission2 OR ...)
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; }
    public PermissionRequirement(params string[] permissions)
    {
        Permissions = permissions.ToList().AsReadOnly();
    }
}

/// <summary>
/// Handler kiểm tra quyền: user cần có ÍT NHẤT MỘT permission trong danh sách
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // User cần có ÍT NHẤT một trong các permissions yêu cầu
        foreach (var perm in requirement.Permissions)
        {
            if (context.User.HasClaim("Permission", perm))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Attribute yêu cầu user có ÍT NHẤT MỘT permission trong danh sách.
/// Dùng cho actions cần kiểm tra quyền theo OR logic (ví dụ: create HOẶC edit).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizePermissionAttribute : AuthorizeAttribute
{
    public AuthorizePermissionAttribute(params string[] permissions)
    {
        // Tạo policy name duy nhất cho tập permissions này
        var sortedPerms = permissions.OrderBy(p => p).ToList();
        var combinedKey = string.Join("_", sortedPerms).Replace(":", "_");
        Policy = "Permission_" + combinedKey;
    }
}

/// <summary>
/// Attribute yêu cầu user phải có TẤT CẢ permissions trong danh sách.
/// Dùng cho actions cần kiểm tra quyền theo AND logic.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeAllPermissionsAttribute : AuthorizeAttribute
{
    public AuthorizeAllPermissionsAttribute(params string[] permissions)
    {
        var sortedPerms = permissions.OrderBy(p => p).ToList();
        var combinedKey = string.Join("_", sortedPerms).Replace(":", "_");
        Policy = "AllPermissions_" + combinedKey;
    }
}
