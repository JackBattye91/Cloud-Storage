using CloudStorage.API.V2.Models;
using JB.Common;
using JB.NoSqlDatabase;
using Microsoft.Extensions.Options;

namespace CloudStorage.API.V2.Repos
{
    public interface IRoleRepo
    {
        Task<Role> CreateAsync(Role user);
        Task<Role> GetByIdAsync(string id);
        Task<Role> GetByNameAsync(string name);
        Task<Role> UpdateAsync(Role user);
        Task DeleteAsync(Role user);
    }

    public class RoleRepo : IRoleRepo
    {
        private readonly ILogger<RoleRepo> _logger;
        private readonly INoSqlDatabaseService _noSql;
        private readonly AppSettings _appSettings;

        public RoleRepo(ILogger<RoleRepo> logger, INoSqlDatabaseService noSql, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _noSql = noSql;
            _appSettings = appSettings.Value;
        }

        public async Task<Role> CreateAsync(Role pRole)
        {
            pRole.Id = Guid.NewGuid().ToString();
            var addRoleRc = await _noSql.AddItem<Role>(_appSettings.Database.Database, Consts.Database.RoleContainer, pRole);

            if (addRoleRc.Success)
            {
                return pRole;
            }

            throw new Exception("Unable to create role");
        }

        public async Task DeleteAsync(Role pRole)
        {
            var deleteRoleRc = await _noSql.DeleteItem<Role>(_appSettings.Database.Database, Consts.Database.RoleContainer, pRole.Id, pRole.Id);

            if (deleteRoleRc.Success)
            {
                return;
            }

            throw new Exception("Unable to delete role");
        }

        public async Task<Role> GetByIdAsync(string id)
        {
            var addRoleRc = await _noSql.GetItem<Role>(_appSettings.Database.Database, Consts.Database.RoleContainer, id);

            if (addRoleRc.Success)
            {
                return addRoleRc.Data!;
            }

            throw new Exception("Unable to get role");
        }

        public async Task<Role> GetByNameAsync(string name)
        {
            string query = $"SELECT * FROM c WHERE c.Name = {name}";
            var getRoleRc = await _noSql.GetItems<Role>(_appSettings.Database.Database, Consts.Database.RoleContainer, query);

            if (getRoleRc.Success)
            {
                if (getRoleRc.Data!.Count == 1)
                {
                    return getRoleRc.Data[0];
                }
            }

            throw new Exception("Unable to get role");
        }

        public async Task<Role> UpdateAsync(Role pRole)
        {
            var getRoleRc = await _noSql.UpdateItem<Role>(_appSettings.Database.Database, Consts.Database.RoleContainer, pRole, pRole.Id, pRole.Id);

            if (getRoleRc.Success)
            {
                return getRoleRc.Data!;
            }

            throw new Exception("Unable to update role");
        }
    }
}
