using CloudStorage.API.V2.Models.DTOs;
using CloudStorage.API.V2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Security;
using Microsoft.Extensions.Options;

namespace CloudStorage.API.V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserService _userService;
        private readonly SignInManager<User> _signInManager;
        private readonly AppSettings _appSettings;

        public UserController(ILogger<UserController> logger, IUserService userService, SignInManager<User> signInManager, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _userService = userService;
            _signInManager = signInManager;
            _appSettings = appSettings.Value;
        }
    }
}
