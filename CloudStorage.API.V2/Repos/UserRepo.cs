using CloudStorage.API.V2.Models;


namespace CloudStorage.API.V2.Repos
{
    public interface IUserRepo
    {
        Task<User> CreateAsync(User user);
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUsernameAsync(string username);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(User user);
    }

    public class UserRepo : IUserRepo
    {
        private readonly ILogger<UserRepo> _logger;

        public UserRepo(ILogger<UserRepo> logger)
        {
            _logger = logger;
        }

        public Task<User> CreateAsync(User user)
        {
            try
            {
                user.Id = Guid.NewGuid().ToString();


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create User Failed");
                throw;
            }

            return Task.FromResult(user);
        }

        public Task DeleteAsync(User user)
        {
            return Task.CompletedTask;
        }

        public async Task<User> GetByIdAsync(string id)
        {
            return await Task.FromResult(new User { Id = id });
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await Task.FromResult(new User { 
                Username = username,
                Forenames = "Jack",
                Surname = "Battye",
                Email = "jack.battye@hotmail.co.uk",
                Id = Guid.NewGuid().ToString()
            });
        }

        public async Task<User> UpdateAsync(User user)
        {
            return await Task.FromResult(user);
        }
    }
}
