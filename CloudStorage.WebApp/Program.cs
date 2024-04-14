using CloudStorage.WebApp.Models;
using CloudStorage.WebApp.Services;
using Microsoft.AspNetCore.DataProtection;

namespace CloudStorage.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            IConfiguration config = builder.Configuration;
            AppSettings? settings = config.Get<AppSettings>();

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddHttpClient("api", options => { options.BaseAddress = new Uri(settings?.ApiBaseUrl ?? ""); }).AddHttpMessageHandler<JwtDelegationHandler>();
            builder.Services.AddTransient<IBlobService, BlobService>();

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

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
