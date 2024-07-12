using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Repos;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace CloudStorage.API.V2.Stores
{
    public class RoleStore : IRoleStore<Role>
    {
        private readonly ILogger<RoleStore> _logger;
        private readonly IRoleRepo _roleRepo;

        public RoleStore(ILogger<RoleStore> logger, IRoleRepo roleRepo)
        {
            _logger = logger;
            _roleRepo = roleRepo;
        }

        public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
        {
            await _roleRepo.CreateAsync(role);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
        {
            await _roleRepo.DeleteAsync(role);
            return IdentityResult.Success;
        }

        public void Dispose()
        {
            
        }

        public async Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return await _roleRepo.GetByIdAsync(roleId);
        }

        public async Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return await _roleRepo.GetByNameAsync(normalizedRoleName);
        }

        public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(role.Name.ToLower());
        }

        public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id);
        }

        public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(role.Name);
        }

        public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName ?? "";
            await _roleRepo.UpdateAsync(role);
        }

        public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
        {
            await _roleRepo.UpdateAsync(role);
            return IdentityResult.Success;
        }
    }
}
