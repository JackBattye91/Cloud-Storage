using CloudStorage.API.V2.Models;
using JB.Common;

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
        private readonly JB.NoSqlDatabase.IWrapper _noSql;

        public RoleRepo(JB.NoSqlDatabase.IWrapper noSql)
        {
            _noSql = noSql;
        }

        public async Task<Role> CreateAsync(Role user)
        {
            IReturnCode rc = new ReturnCode();
            Role? createdRole = null;

            try
            {
                if (rc.Success)
                {
                    var addRoleRc = await _noSql.AddItem("", "", user);

                    if (addRoleRc.Success)
                    {
                        createdRole = addRoleRc.Data;
                    }

                    if (addRoleRc.Failed)
                    {
                        ErrorWorker.CopyErrors(addRoleRc, rc);
                        throw new JB.Common.Errors.JBException("Unable to create Role");
                    }
                }


                if (rc.Success && createdRole != null)
                {
                    return createdRole;
                }
            }
            catch (Exception ex)
            {
                rc.AddError(new Error(ex));
                throw;
            }
        }

        public Task DeleteAsync(Role user)
        {
            throw new NotImplementedException();
        }

        public Task<Role> GetByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<Role> GetByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<Role> UpdateAsync(Role user)
        {
            throw new NotImplementedException();
        }
    }
}
