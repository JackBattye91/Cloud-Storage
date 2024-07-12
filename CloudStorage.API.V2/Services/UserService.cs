using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Repos;

namespace CloudStorage.API.V2.Services
{
    public interface IUserService
    {
        Task<User> CreateAsync(User user);
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(User user);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepo _userRepo;
        public UserService(IUserRepo userRepo)
        {
            _userRepo = userRepo;
        }

        public Task<User> CreateAsync(User user)
        {
            return _userRepo.CreateAsync(user);
        }

        public Task DeleteAsync(User user)
        {
            return _userRepo.DeleteAsync(user);
        }

        public Task<User> GetByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetByUsernameAsync(string username)
        {
            return _userRepo.GetByUsernameAsync(username);
        }

        public Task<User> GetByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<User> UpdateAsync(User user)
        {
            throw new NotImplementedException();
        }
    }
}
