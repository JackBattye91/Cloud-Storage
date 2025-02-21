using Azure;
using CloudStorage.API.Models;
using CloudStorage.Interfaces;
using CloudStorage.Models;
using Microsoft.Azure.Cosmos;

namespace CloudStorage.API.Services
{
    public interface IDatabaseService
    {
        // Users
        Task<IUser> GetUserByUsernameAsync(string username);
        Task<IUser> GetUserByIdAsync(string id);
        Task CreateUserAsync(IUser user);
        Task<IUser> UpdateUserAsync(IUser user);
        Task DeleteUserAsync(string userId);
        Task<bool> DoesUsernameExist(string username);
        Task<bool> DoesEmailExist(string email);

        // Refresh Tokens
        Task InsertRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken> GetRefreshTokenAsync(string id);
        Task<IEnumerable<RefreshToken>> GetRefreshTokensByUserIdAsync(string userId);
        Task DeleteRefreshTokenAsync(RefreshToken refreshToken);


        // Blobs
        Task<IEnumerable<BlobDetail>> GetBlobDetailsByUserIdAsync(string userId);
        Task<BlobDetail> GetBlobDetailsByIdAsync(string id, string userId);
        Task CreateBlobDetailsByIdAsync(IBlobDetail blobDetail);
        Task UpdateBlobDetail(IBlobDetail blobDetail);
        
    }

    public class CosmosService : IDatabaseService
    {
        private readonly ILogger<CosmosService> _logger;
        private readonly CosmosClient _client;
        private readonly AppSettings _settings;

        public CosmosService(ILogger<CosmosService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _settings = configuration.Get<AppSettings>() ?? throw new Exception("Unable to get AppSettings");
            _client = new CosmosClient(_settings.Database.ConnectionString);
        }

        #region USERS
        public async Task<IUser> GetUserByUsernameAsync(string username)
        {
            try
            {
                IUser? user = null;
                Container usersContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.USER_CONTAINER_NAME);

                QueryDefinition queryDefinition = new QueryDefinition($"SELECT * FROM c WHERE c.username='{username}'");
                using (FeedIterator<CloudStorage.Models.User>? feedIterator = usersContainer?.GetItemQueryIterator<CloudStorage.Models.User>(queryDefinition))
                {
                    if (feedIterator?.HasMoreResults == true)
                    {
                        FeedResponse<CloudStorage.Models.User> resultSet = await feedIterator.ReadNextAsync();
                        user = resultSet.FirstOrDefault();
                    }
                }

                if (user == null)
                {
                    throw new Exception($"Unable to find user with username: {username}");
                }

                return user;
            }
            catch (Exception ex) { 
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        public async Task<IUser> GetUserByIdAsync(string id)
        {
            try
            {
                IUser? user = null;
                Container usersContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.USER_CONTAINER_NAME);

                QueryDefinition queryDefinition = new QueryDefinition($"SELECT * FROM c WHERE c.id='{id}'");
                using (FeedIterator<CloudStorage.Models.User>? feedIterator = usersContainer?.GetItemQueryIterator<CloudStorage.Models.User>(queryDefinition))
                {
                    if (feedIterator?.HasMoreResults == true)
                    {
                        FeedResponse<CloudStorage.Models.User> resultSet = await feedIterator.ReadNextAsync();
                        user = resultSet.FirstOrDefault();
                    }
                }

                if (user == null)
                {
                    throw new Exception($"Unable to find user with id: {id}");
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task CreateUserAsync(IUser user)
        {
            try
            {
                Container refreshContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.USER_CONTAINER_NAME);
                ItemResponse<IUser> response = await refreshContainer.CreateItemAsync(user, new PartitionKey(user.Id));

                if (response.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    throw new Exception("Unable to insert user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<IUser> UpdateUserAsync(IUser user)
        {
            try
            {
                Container refreshContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.USER_CONTAINER_NAME);
                ItemResponse<IUser> response = await refreshContainer.UpsertItemAsync(user, new PartitionKey(user.Id));

                if (response.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    throw new Exception("Unable to insert user");
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            try
            {
                Container refreshContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.USER_CONTAINER_NAME);
                ItemResponse<IUser> response = await refreshContainer.DeleteItemAsync<IUser>(userId, new PartitionKey(userId));

                if (response.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    throw new Exception("Unable to insert user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<bool> DoesUsernameExist(string username)
        {
            try
            {
                Container userContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.USER_CONTAINER_NAME);
                QueryDefinition query = new QueryDefinition($"SELECT * FROM c WHERE c.username = '{username}'");

                using (FeedIterator<CloudStorage.Models.User>? feedIterator = userContainer.GetItemQueryIterator<CloudStorage.Models.User>(query))
                {
                    if (feedIterator.HasMoreResults == true)
                    {
                        FeedResponse<CloudStorage.Models.User> resultSet = await feedIterator.ReadNextAsync();
                        IUser? user = resultSet.FirstOrDefault();

                        if (string.Equals(user?.Username, username, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        public async Task<bool> DoesEmailExist(string email)
        {
            try
            {
                Container userContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.USER_CONTAINER_NAME);
                QueryDefinition query = new QueryDefinition($"SELECT * FROM c WHERE c.email = '{email}'");

                using (FeedIterator<CloudStorage.Models.User>? feedIterator = userContainer.GetItemQueryIterator<CloudStorage.Models.User>(query))
                {
                    if (feedIterator.HasMoreResults == true)
                    {
                        FeedResponse<CloudStorage.Models.User> resultSet = await feedIterator.ReadNextAsync();
                        IUser? user = resultSet.FirstOrDefault();

                        if (string.Equals(user?.Email, email, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        #endregion

        #region REFRESH TOKENS
        public async Task InsertRefreshTokenAsync(RefreshToken refreshToken)
        {
            try
            {
                Container refreshContainer = _client.GetContainer (_settings.Database.DatabaseName, Consts.Database.REFRESH_TOKEN_CONTAINER_NAME);
                ItemResponse<RefreshToken> response = await refreshContainer.CreateItemAsync(refreshToken, new PartitionKey(refreshToken.UserId));

                if (response.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    throw new Exception("Unable to insert Refresh Token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        public async Task<RefreshToken> GetRefreshTokenAsync(string id)
        {
            try
            {
                RefreshToken? refreshToken = null;
                Container refreshContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.REFRESH_TOKEN_CONTAINER_NAME);
                QueryDefinition query = new QueryDefinition($"SELECT * FROM c WHERE c.id = '{id}'");
                using (FeedIterator<RefreshToken> feedIterator = refreshContainer.GetItemQueryIterator<RefreshToken>(query))
                {
                    if (feedIterator?.HasMoreResults == true)
                    {
                        FeedResponse<RefreshToken> resultSet = await feedIterator.ReadNextAsync();
                        refreshToken = resultSet.FirstOrDefault();
                    }
                }

                if (refreshToken == null)
                {
                    throw new Exception("Unable to find refresh token");
                }

                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<RefreshToken>> GetRefreshTokensByUserIdAsync(string userId)
        {
            try
            {
                List<RefreshToken> refreshTokens = new List<RefreshToken>();
                Container refreshContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.REFRESH_TOKEN_CONTAINER_NAME);
                QueryDefinition query = new QueryDefinition($"SELECT * FROM c WHERE c.UserId = '{userId}'");
                using (FeedIterator<RefreshToken> feedIterator = refreshContainer.GetItemQueryIterator<RefreshToken>(query))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        FeedResponse<RefreshToken> resultSet = await feedIterator.ReadNextAsync();
                        refreshTokens.Add(resultSet.First());
                    }
                }

                return refreshTokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task DeleteRefreshTokenAsync(RefreshToken refreshToken)
        {
            try
            {
                Container refreshContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.REFRESH_TOKEN_CONTAINER_NAME);
                ItemResponse<RefreshToken> response = await refreshContainer.DeleteItemAsync<RefreshToken>(refreshToken.Id, new PartitionKey(refreshToken.UserId));

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("Unable to delete Refresh Token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        #endregion

        #region BLOBS
        public async Task<IEnumerable<BlobDetail>> GetBlobDetailsByUserIdAsync(string userId)
        {
            try
            {
                List<BlobDetail> blobDetails = new List<BlobDetail>();
                Container blobContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.PICTURES_CONTAINER_NAME);
                QueryDefinition query = new QueryDefinition($"SELECT * FROM c WHERE c.userId = '{userId}'");
                using (FeedIterator<BlobDetail> feedIterator = blobContainer.GetItemQueryIterator<BlobDetail>(query))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        FeedResponse<BlobDetail> resultSet = await feedIterator.ReadNextAsync();
                        blobDetails.Add(resultSet.First());
                    }
                }

                return blobDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task<BlobDetail> GetBlobDetailsByIdAsync(string id, string userId)
        {
            try
            {
                BlobDetail? blobDetail = null;
                Container blobContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.PICTURES_CONTAINER_NAME);
                QueryDefinition query = new QueryDefinition($"SELECT * FROM c WHERE c.id = '{id}' AND c.userId = '{userId}' AND c.deleted = false");
                using (FeedIterator<BlobDetail> feedIterator = blobContainer.GetItemQueryIterator<BlobDetail>(query))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        FeedResponse<BlobDetail> resultSet = await feedIterator.ReadNextAsync();
                        blobDetail = resultSet.FirstOrDefault();
                    }
                }

                if (blobDetail == null)
                {
                    throw new Exception("Unable to find blob details");
                }

                return blobDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task CreateBlobDetailsByIdAsync(IBlobDetail blobDetail)
        {
            try
            {
                Container blobContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.PICTURES_CONTAINER_NAME);
                ItemResponse<IBlobDetail> response = await blobContainer.CreateItemAsync(blobDetail, new PartitionKey(blobDetail.UserId));

                if (response.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    throw new Exception("Unable to insert Blob Detail");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        public async Task UpdateBlobDetail(IBlobDetail blobDetail)
        {
            try
            {
                Container blobContainer = _client.GetContainer(_settings.Database.DatabaseName, Consts.Database.PICTURES_CONTAINER_NAME);
                ItemResponse<IBlobDetail> response = await blobContainer.UpsertItemAsync(blobDetail, new PartitionKey(blobDetail.UserId));

                if (response.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    throw new Exception("Unable to update Blob Detail");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
