using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Repos;

namespace CloudStorage.API.V2.Services
{
    public interface IUserService
    {
        Task<User> CreateAsync(User user);
        Task<User> ActivteAccountAsync(string activationKey);
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(User user);

        Task<bool>ValidationRefreshTokenAsync(string email, string refreshToken);
        Task<RefreshToken>CreateRefreshTokenAsync(User user);
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

        public async Task<User> ActivteAccountAsync(string activationKey)
        {
            AccountActivationKey? accountActivationKey = await _userRepo.GetActivationKeyAsync(activationKey);

            if (accountActivationKey == null)
            {
                throw new Exception("Unable to find activation key");
            }

            if (DateTime.UtcNow < accountActivationKey.Expires) {
                User user = await _userRepo.GetByIdAsync(accountActivationKey.UserId);
                user.Activated = true;
                await UpdateAsync(user);

                return user;
            }
            else
            {
                throw new Exception("Account Activation Key has expired");
            }

            throw new Exception("Unable to activate account");
        }

        public Task DeleteAsync(User user)
        {
            return _userRepo.DeleteAsync(user);
        }

        public Task<User> GetByIdAsync(string id)
        {
            return _userRepo.GetByIdAsync(id);
        }

        public Task<User> GetByUsernameAsync(string username)
        {
            return _userRepo.GetByUsernameAsync(username);
        }

        public Task<User> GetByEmailAsync(string email)
        {
            return _userRepo.GetByEmailAsync(email);
        }

        public Task<User> UpdateAsync(User user)
        {
            return _userRepo.UpdateAsync(user);
        }


        public async Task<bool> ValidationRefreshTokenAsync(string email, string refreshToken)
        {
            IEnumerable<RefreshToken> refreshTokens = await _userRepo.GetRefreshTokensAsync(email);

            if (refreshTokens.Where(x => x.Id == refreshToken && x.Expires > DateTime.UtcNow).Any())
            {
                return true;
            }

            return false;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(User user)
        {
            return await _userRepo.CreateRefreshToken(user);
        }
    }
}
