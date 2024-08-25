using CloudStorage.SPA.V2.Components;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CloudStorage.SPA.V2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents();

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o => {
                o.RequireHttpsMetadata = false;
                o.SaveToken = true;
                o.TokenValidationParameters = TokenUtilities.ValidationParameters(appSettings);
            });
            builder.Services.AddAuthorization(config =>
            {
                config.AddPolicy("Password", policy => policy.RequireClaim("amr", "pwd", "mfa"));
                config.AddPolicy("MFA", policy => policy.RequireClaim("amr", "mfa"));
                config.AddPolicy("Full", policy => policy.RequireClaim("adm", "true"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorComponents<App>();

            app.Run();
        }
    }
}
