using CloudStorage.Interfaces;
using CloudStorage.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace CloudStorage.WebApp.Models
{
    public class AppState
    {
        public static AppState? Instance { get; set; }
        public IUser? User { get; set; }
        public IToken? Token { get; set; }

        public static async Task Load(ProtectedLocalStorage pProtectedStorage)
        {
            if (Instance == null)
            {
                Instance = new AppState();
            }

            try
            {
                if (Instance.User == null)
                {
                    ProtectedBrowserStorageResult<User>? userValue = await pProtectedStorage.GetAsync<User>("user");
                    Instance!.User = userValue?.Value;
                }

                if (Instance.Token == null)
                {
                    ProtectedBrowserStorageResult<Token>? tokenValue = await pProtectedStorage.GetAsync<Token>("token");
                    Instance!.Token = tokenValue?.Value;
                }
                
            }
            catch (Exception ex)
            {

            }
        }
    }
}
