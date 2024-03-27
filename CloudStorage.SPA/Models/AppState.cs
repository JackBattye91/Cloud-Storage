using CloudStorage.Interfaces;
using CloudStorage.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace CloudStorage.SPA.Models
{
    public class AppState
    {
        public static AppState? Instance { get; set; }
        public IUser? CurrentUser { get; set; }
        //public IList<IBlobDetail>? BlobDetails { get; set; } = null;
        public IToken? Token { get; set; }

        public static async Task Load(ProtectedLocalStorage pProtectedStorage)
        {
            if (Instance == null)
            {
                Instance = new AppState();
            }

            try
            {
                ProtectedBrowserStorageResult<User>? userValue = await pProtectedStorage.GetAsync<User>("user");
                Instance!.CurrentUser = userValue?.Value;

                ProtectedBrowserStorageResult<Token>? tokenValue = await pProtectedStorage.GetAsync<Token>("token");
                Instance!.Token = tokenValue?.Value;
            }
            catch (Exception ex)
            {

            }
        }
    }
}
