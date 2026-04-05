using Newtonsoft.Json;
using SV22T1020161.Models.Security;

namespace SV22T1020161.Admin
{
    /// <summary>
    /// Lớp cung cấp các tiện ích liên quan đến ngữ cảnh của ứng dụng web
    /// </summary>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Gọi hàm này trong Program
        /// </summary>
        /// <param name="httpContextAccessor">app.Services.GetRequiredService<IHttpContextAccessor>()</param>
        /// <param name="webHostEnvironment">app.Services.GetRequiredService<IWebHostEnvironment>()</param>
        /// <param name="configuration"><app.Configuration/param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Configure(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException();
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException();
            _configuration = configuration ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// HttpContext
        /// </summary>
        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;
        /// <summary>
        /// WebHostEnviroment
        /// </summary>
        public static IWebHostEnvironment? WebHostEnviroment => _webHostEnvironment;
        /// <summary>
        /// Configuration
        /// </summary>
        public static IConfiguration? Configuration => _configuration;

        /// <summary>
        /// URL của website, kết thúc bởi dấu / (ví dụ: https://mywebsite.com/)
        /// </summary>
        public static string BaseURL => $"{HttpContext?.Request.Scheme}://{HttpContext?.Request.Host}/";
        /// <summary>
        /// Đường dẫn vật lý đến thư mục wwwroot
        /// </summary>
        public static string WWWRootPath => _webHostEnvironment?.WebRootPath ?? string.Empty;
        /// <summary>
        /// Đường dẫn vật lý đến thư mục gốc lưu ứng dụng Web
        /// </summary>
        public static string ApplicationRootPath => _webHostEnvironment?.ContentRootPath ?? string.Empty;        

        /// <summary>
        /// Ghi dữ liệu vào session
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetSessionData(string key, object value)
        {
            try
            {
                string sValue = JsonConvert.SerializeObject(value);
                if (!string.IsNullOrEmpty(sValue))
                {
                    _httpContextAccessor?.HttpContext?.Session.SetString(key, sValue);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Đọc dữ liệu từ session
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetSessionData<T>(string key) where T : class
        {
            try
            {
                string sValue = _httpContextAccessor?.HttpContext?.Session.GetString(key) ?? "";
                if (!string.IsNullOrEmpty(sValue))
                {
                    return JsonConvert.DeserializeObject<T>(sValue);
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Lấy chuỗi giá trị của cấu hình trong appsettings.json
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetConfigValue(string name)
        {
            return _configuration?[name] ?? "";
        }

        /// <summary>
        /// Lấy đối tượng có kiểu là T trong phần cấu hình có tên là name trong appsettings.json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetConfigSection<T>(string name) where T : new()
        {
            var value = new T();
            _configuration?.GetSection(name).Bind(value);
            return value;
        }

        /// <summary>
        /// Lấy WebUser của người dùng hiện tại từ HttpContext.
        /// Cung cấp các phương thức kiểm tra quyền.
        /// </summary>
        public static WebUser? CurrentUser
        {
            get
            {
                var user = HttpContext?.User;
                if (user == null || user.Identity?.IsAuthenticated != true)
                    return null;
                return new WebUser(user);
            }
        }

        /// <summary>
        /// Kiểm tra nhanh người dùng hiện tại có quyền cụ thể hay không.
        /// </summary>
        /// <param name="permission">Mã quyền</param>
        public static bool HasPermission(string permission)
        {
            return CurrentUser?.HasPermission(permission) ?? false;
        }

        /// <summary>
        /// Kiểm tra nhanh người dùng hiện tại có vai trò cụ thể hay không.
        /// </summary>
        /// <param name="role">Tên vai trò</param>
        public static bool HasRole(string role)
        {
            return CurrentUser?.HasRole(role) ?? false;
        }
    }
}
