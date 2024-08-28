using Microsoft.AspNetCore.Identity;
using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Services;

namespace CloudStorage.API.V2.Stores
{
    public class UserStore : IUserStore<User>, IUserPasswordStore<User>
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserStore> _logger;

        public UserStore(ILogger<UserStore> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            await _userService.CreateAsync(user);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            await _userService.DeleteAsync(user);
            return IdentityResult.Success;
        }

        public void Dispose()
        {
            
        }

        public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await _userService.GetByIdAsync(userId);
        }

        public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await _userService.GetByIdAsync(normalizedUserName);
        }

        public async Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return await Task.FromResult(user.Username.ToLower());
        }

        public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(user.Password);
        }

        public async Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            return await Task.FromResult(user.Id);
        }

        public async Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return await Task.FromResult(user.Username);
        }

        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.Password));
        }

        public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.Password = passwordHash ?? string.Empty;
            await _userService.UpdateAsync(user);
        }

        public async Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
        {
            user.Username = userName ?? "";
            await _userService.UpdateAsync(user);
        }

        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            await _userService.UpdateAsync(user);
            return IdentityResult.Success;
        }
    }
}
