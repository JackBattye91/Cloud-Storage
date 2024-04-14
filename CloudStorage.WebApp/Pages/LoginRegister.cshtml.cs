using CloudStorage.Interfaces;
using CloudStorage.Models;
using CloudStorage.WebApp.Models;
using CloudStorage.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;

namespace CloudStorage.WebApp.Pages
{
    public class LoginRegisterModel : PageModel
    {
        ILogger<LoginRegisterModel> _logger;
        NavigationManager _navManager;

        public IUser? User { get; private set; } = null;

        public LoginRegisterModel(ILogger<LoginRegisterModel> logger, NavigationManager navManager, CookieService cookieService)
        {
            _logger = logger;
            _navManager = navManager;
        }

        public async void OnGet()
        {
            try
            {
                User = AppState.Instance?.User;

                if (User != null)
                {
                    _navManager.NavigateTo("/", true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}
