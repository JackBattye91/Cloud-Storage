using CloudStorage.API.V2.Models;
using Microsoft.Extensions.Options;

namespace CloudStorage.API.V2.Services
{
    public interface IBlobDetailService
    {
        Task<BlobDetail> Get(string id);
        Task<IList<BlobDetail>> GetAll();
        Task<IList<BlobDetail>> GetByUser(string userId);
        Task<BlobDetail> Create(BlobDetail blobDetail);
        Task<BlobDetail> Update(BlobDetail blobDetail);
        Task Delete(string id);
    }

    public class BlobDetailService : IBlobDetailService
    {
        private readonly JB.NoSqlDatabase.INoSqlDatabaseService _noSqlWrapper;
        private readonly AppSettings _appSettings;

        public BlobDetailService(JB.NoSqlDatabase.INoSqlDatabaseService noSqlWrapper, IOptions<AppSettings> appSettings)
        {
            _noSqlWrapper = noSqlWrapper;
            _appSettings = appSettings.Value;
        }

        public async Task<BlobDetail> Create(BlobDetail blobDetail)
        {
            var rc = await _noSqlWrapper.AddItem(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, blobDetail);

            if (rc.Success)
            {
                return rc.Data!;
            }

            if (rc.Failed)
            {
                Exception? ex = rc.Errors?.FirstOrDefault()?.Exception;

                if (ex != null)
                {
                    throw ex;
                }
            }

            throw new Exception("Unable to create BlobDetails");
        }

        public async Task Delete(string id)
        {
            var blobDetail = await Get(id);
            blobDetail.Deleted = true;
            await Update(blobDetail);
        }

        public async Task<BlobDetail> Get(string id)
        {
            var rc = await _noSqlWrapper.GetItem<BlobDetail>(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, id);

            if (rc.Success)
            {
                return rc.Data!;
            }

            if (rc.Failed)
            {
                Exception? ex = rc.Errors?.FirstOrDefault()?.Exception;

                if (ex != null)
                {
                    throw ex;
                }
            }

            throw new Exception("Unable to create BlobDetails");
        }

        public async Task<IList<BlobDetail>> GetAll()
        {
            var rc = await _noSqlWrapper.GetItems<BlobDetail>(_appSettings.Database.Database, Consts.Database.BlobDetailContainer);

            if (rc.Success)
            {
                return rc.Data!;
            }

            if (rc.Failed)
            {
                Exception? ex = rc.Errors?.FirstOrDefault()?.Exception;

                if (ex != null)
                {
                    throw ex;
                }
            }

            throw new Exception("Unable to get all BlobDetails");
        }

        public async Task<IList<BlobDetail>> GetByUser(string userId)
        {
            string query = $"SELECT * FROM c WHERE c.UserId = '{userId}' AND c.Deleted = false";
            var rc = await _noSqlWrapper.GetItems<BlobDetail>(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, query);

            if (rc.Success)
            {
                return rc.Data!;
            }

            if (rc.Failed)
            {
                Exception? ex = rc.Errors?.FirstOrDefault()?.Exception;

                if (ex != null)
                {
                    throw ex;
                }
            }

            throw new Exception("Unable to get BlobDetails");
        }

        public async Task<BlobDetail> Update(BlobDetail blobDetail)
        {
            var rc = await _noSqlWrapper.UpdateItem<BlobDetail>(_appSettings.Database.Database, Consts.Database.BlobDetailContainer, blobDetail, blobDetail.Id, blobDetail.UserId);

            if (rc.Success)
            {
                return rc.Data!;
            }

            if (rc.Failed)
            {
                Exception? ex = rc.Errors?.FirstOrDefault()?.Exception;

                if (ex != null)
                {
                    throw ex;
                }
            }

            throw new Exception("Unable to get BlobDetails");
        }
    }
}
