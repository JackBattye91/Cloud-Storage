using CloudStorage.SPA.V2.Components;
using CloudStorage.SPA.V2.Models;
using CloudStorage.SPA.V2.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MudBlazor.Services;

namespace CloudStorage.SPA.V2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            AppSettings appSettings = builder.Configuration.Get<AppSettings>() ?? new AppSettings();

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o => {
                o.RequireHttpsMetadata = false;
                o.SaveToken = true;
            });
            builder.Services.AddAuthorization(config =>
            {
                config.AddPolicy("Password", policy => policy.RequireClaim("amr", "pwd", "mfa"));
                config.AddPolicy("MFA", policy => policy.RequireClaim("amr", "mfa"));
                config.AddPolicy("Full", policy => policy.RequireClaim("adm", "true"));
            });

            builder.Services.AddTransient<AuthenticationService>();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient<CloudHttpClient>("api", config => config.BaseAddress = new Uri(appSettings.BaseApiUrl));
            builder.Services.AddSingleton<BlobService>();
            builder.Services.AddMudServices();

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

            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
