using System.Security.Claims;

namespace WarehouseUserManagement.Services
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? Username { get; }
        string? FullName { get; }
        int? RoleId { get; }
        string? RoleName { get; }
        bool IsAuthenticated { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly ClaimsPrincipal? _user;

        public CurrentUserService(IHttpContextAccessor accessor)
        {
            _user = accessor.HttpContext?.User;
        }

        public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

        public int? UserId
        {
            get
            {
                var value = _user?.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(value, out var id) ? id : null;
            }
        }

        public string? Username => _user?.FindFirstValue(ClaimTypes.Name);
        public string? FullName => _user?.FindFirstValue("FullName");

        public int? RoleId
        {
            get
            {
                var value = _user?.FindFirstValue("RoleId");
                return int.TryParse(value, out var id) ? id : null;
            }
        }

        public string? RoleName => _user?.FindFirstValue(ClaimTypes.Role);
    }
}
