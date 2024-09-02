﻿using CloudStorage.API.V2.Models;
using JB.Common;
using JB.NoSqlDatabase;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Errors.Model;


namespace CloudStorage.API.V2.Repos
{
    public interface IUserRepo
    {
        Task<User> CreateAsync(User user);
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<AccountActivationKey?> GetActivationKeyAsync(string activationKey);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(User user);

        Task<IEnumerable<RefreshToken>> GetRefreshTokensAsync(string email);
        Task<RefreshToken> CreateRefreshToken(User user);
    }

    public class UserRepo : IUserRepo
    {
        private readonly ILogger<UserRepo> _logger;
        private readonly INoSqlDatabaseService _noSqlWrapper;
        private readonly AppSettings _appSettings;

        public UserRepo(ILogger<UserRepo> logger, INoSqlDatabaseService noSqlWrapper, IOptions<AppSettings> options)
        {
            _logger = logger;
            _noSqlWrapper = noSqlWrapper;
            _appSettings = options.Value;
        }

        public async Task<User> CreateAsync(User user)
        {
            IReturnCode<User> createRefreshTokens = await _noSqlWrapper.AddItem<User>(_appSettings.Database.Database, Consts.Database.UserContainer, user);

            if (createRefreshTokens.Failed)
            {
                throw new Exception("Unable to create user");
            }

            return user;
        }

        public async Task<AccountActivationKey?> GetActivationKeyAsync(string activationKey)
        {
            string query = $"SELECT * FROM c WHERE c.ActivationKey = '{activationKey}'";
            IReturnCode<IList<AccountActivationKey>> getActivationKeysRc = await _noSqlWrapper.GetItems<AccountActivationKey>(_appSettings.Database.Database, Consts.Database.ACTIVATION_KEY, query);

            if (getActivationKeysRc.Success)
            {
                if (getActivationKeysRc.Data?.Count > 0)
                {
                    return getActivationKeysRc.Data[0];
                }
            }
            if (getActivationKeysRc.Failed)
            {
                throw new Exception("Unable to get activation key");
            }

            return null;
        }

        public async Task DeleteAsync(User user)
        {
            await _noSqlWrapper.DeleteItem<User>(_appSettings.Database.Database, Consts.Database.UserContainer, user.Id, user.Id);
        }

        public async Task<User> GetByIdAsync(string id)
        {
            string query = $"SELECT * FROM c WHERE c.id = '{id}'";
            var getUserRc = await _noSqlWrapper.GetItems<User>(_appSettings.Database.Database, Consts.Database.UserContainer, query);

            if (getUserRc.Success)
            {
                if (getUserRc.Data?.Count == 1)
                {
                    return getUserRc.Data[0];
                }
            }
            return new User();
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            string query = $"SELECT * FROM c WHERE c.Username = '{username}'";
            var getUserRc = await _noSqlWrapper.GetItems<User>(_appSettings.Database.Database, Consts.Database.UserContainer, query);

            if (getUserRc.Success)
            {
                if (getUserRc.Data?.Count == 1)
                {
                    return getUserRc.Data[0];
                }
            }

            return new User();
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            string query = $"SELECT * FROM c WHERE c.Email = '{email}'";
            var getUserRc = await _noSqlWrapper.GetItems<User>(_appSettings.Database.Database, Consts.Database.UserContainer, query);

            if (getUserRc.Success)
            {
                if (getUserRc.Data?.Count == 1)
                {
                    return getUserRc.Data[0];
                }
            }

            return new User();
        }

        public async Task<User> UpdateAsync(User user)
        {
            var updateUser = await _noSqlWrapper.UpdateItem<User>(_appSettings.Database.Database, Consts.Database.UserContainer, user, user.Id, user.Id);

            if (updateUser.Failed)
            {
                throw new Exception("Unable to update user");
            }

            return user;
        }

        public async Task<IEnumerable<RefreshToken>> GetRefreshTokensAsync(string email)
        {
            IReturnCode<IList<RefreshToken>> getRefreshTokens = await _noSqlWrapper.GetItems<RefreshToken>(_appSettings.Database.Database, Consts.Database.RefreshTokenContainer);

            if (getRefreshTokens.Success)
            {
                return getRefreshTokens.Data!;
            }

            throw new Exception("Unable to get refresh tokens");
        }
        public async Task<RefreshToken> CreateRefreshToken(User user)
        {
            RefreshToken refreshToken = new RefreshToken() { 
                Id = Guid.NewGuid().ToString(),
                Subject = user.Id,
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(30),
            };

            IReturnCode<RefreshToken> createRefreshTokens = await _noSqlWrapper.AddItem<RefreshToken>(_appSettings.Database.Database, Consts.Database.RefreshTokenContainer, refreshToken);

            if (createRefreshTokens.Failed)
            {
                throw new Exception("Unable to create refresh token");
            }

            return refreshToken;
        }
    }
}
