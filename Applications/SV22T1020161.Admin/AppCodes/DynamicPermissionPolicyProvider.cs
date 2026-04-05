using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SV22T1020161.Admin.AppCodes;

/// <summary>
/// Cung cấp Authorization Policy động cho các permission.
/// Hỗ trợ cả policy đơn lẻ ("Permission_x") và policy kết hợp ("Permission_x_y")
/// mà không cần đăng ký trước từng tổ hợp.
/// </summary>
public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PermissionPrefix = "Permission_";
    private const string AllPermissionsPrefix = "AllPermissions_";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Xử lý OR-logic: "Permission_x_y_z" → user cần có ÍT NHẤT MỘT trong các permissions
        if (policyName.StartsWith(PermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var combinedKey = policyName[PermissionPrefix.Length..];
            // Khôi phục lại các permissions: thay "_" → ":" rồi tách theo "_<module>"
            // Thực ra permission values dùng ":" (ví dụ "employee:create"), nhưng policy name
            // đã replace ":" → "_". Ta cần parse ngược lại.
            var permissions = ParsePermissionsFromKey(combinedKey);

            if (permissions.Length > 0)
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(permissions))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        // Delegate sang fallback (xử lý các policy đã đăng ký như "AdminOnly", "ManagerOrAdmin")
        return _fallback.GetPolicyAsync(policyName);
    }

    /// <summary>
    /// Phân tích combinedKey thành danh sách permission strings.
    /// Ví dụ: "employee_create_employee_edit" → ["employee:create", "employee:edit"]
    /// Logic: split theo "_" rồi ghép cặp [module, action] lại với ":" vì mỗi permission
    /// có dạng "{module}:{action}" (2 phần).
    /// </summary>
    private static string[] ParsePermissionsFromKey(string combinedKey)
    {
        // Mỗi permission value có dạng "segment1:segment2" (không dấu gạch dưới nội bộ).
        // Khi tạo policy name: replace ":" → "_" và join bằng "_".
        // Ví dụ: ["employee:create","employee:edit"] → "employee_create_employee_edit"
        // Ta split theo "_" → ["employee","create","employee","edit"] rồi ghép từng cặp.
        var segments = combinedKey.Split('_');
        if (segments.Length % 2 != 0)
        {
            // Không parse được → trả về toàn bộ key như một permission (fallback)
            return new[] { combinedKey.Replace('_', ':') };
        }

        var result = new List<string>();
        for (int i = 0; i < segments.Length; i += 2)
        {
            result.Add($"{segments[i]}:{segments[i + 1]}");
        }
        return result.ToArray();
    }
}
