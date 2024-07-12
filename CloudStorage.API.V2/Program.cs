using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Repos;
using CloudStorage.API.V2.Security;
using CloudStorage.API.V2.Services;
using CloudStorage.API.V2.Stores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CloudStorage.API.V2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            IConfiguration config = builder.Configuration;
            builder.Services.Configure<AppSettings>(config);
            AppSettings? appSettings = config.Get<AppSettings>();

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o => {
                o.RequireHttpsMetadata = false;
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = appSettings!.Jwt.Issuer,
                    ValidAudience = appSettings!.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings!.Jwt.Key)),
                    //LifetimeValidator = new LifetimeValidator(CustomLifetimeValidator.Create),

                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    RequireSignedTokens = true
                };
            });
            builder.Services.AddAuthorization();

            builder.Services.AddIdentity<User, Role>(config => { 
                config.User.RequireUniqueEmail = true;
            }).AddDefaultTokenProviders();

            builder.Services.AddSingleton<IBlobService, BlobService>();
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<IUserRepo, UserRepo>();
            builder.Services.AddSingleton<IRoleRepo, RoleRepo>();

            builder.Services.AddTransient<IUserStore<User>, UserStore>();
            builder.Services.AddTransient<IRoleStore<Role>, RoleStore>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseAuthentication();

            app.MapControllers();

            app.Run();
        }
    }
}
