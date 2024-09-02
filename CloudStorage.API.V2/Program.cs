using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Repos;
using CloudStorage.API.V2.Security;
using CloudStorage.API.V2.Services;
using CloudStorage.API.V2.Stores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JB.Blob;
using JB.Email;
using JB.NoSqlDatabase;

namespace CloudStorage.API.V2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddNewtonsoftJson();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            IConfiguration config = builder.Configuration;
            builder.Services.Configure<AppSettings>(config);
            AppSettings appSettings = config.Get<AppSettings>() ?? new AppSettings();

            builder.Services.ConfigureHttpJsonOptions(config => {
                config.SerializerOptions.AllowTrailingCommas = true;
                config.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
            });

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o => {
                o.RequireHttpsMetadata = false;
                o.SaveToken = false;
                o.TokenValidationParameters = TokenUtilities.ValidationParameters(appSettings);
            });

            builder.Services.AddAuthorization(config =>
            { 
                config.AddPolicy(Consts.Policies.PASSWORD, policy => policy.RequireClaim("amr", "pwd", "mfa"));
                config.AddPolicy(Consts.Policies.MFA, policy => policy.RequireClaim("amr", "mfa"));
                config.AddPolicy(Consts.Policies.ADMIN, policy => policy.RequireClaim("admin", "true"));
            });

            builder.Services.AddIdentity<User, Role>(config => { 
                config.User.RequireUniqueEmail = true;
            }).AddDefaultTokenProviders();

            // JB
            builder.Services.AddNoSqlDatabaseService(appSettings.Database.ConnectionString);
            builder.Services.AddEmailService(appSettings.Email.ApiKey);
            builder.Services.AddBlobService(appSettings.Blob.ConnectionString);

            // LOCAL
            builder.Services.AddScoped<IBlobService, BlobService>();

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
