using CloudStorage.API.V2.Models;

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
        public Task<Role> CreateAsync(Role user)
        {
            throw new NotImplementedException();
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
