using CloudStorage.Models;
using CloudStorage.SPA.Components;
using CloudStorage.SPA.Models;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;
using Blazored.SessionStorage;

var builder = WebApplication.CreateBuilder(args);
AppSettings? appSettings = builder.Configuration.Get<AppSettings>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddHttpClient("api", x => {
    x.BaseAddress = new Uri(appSettings.ApiBaseUrl);
}).AddHttpMessageHandler<JwtDelegationHandler>();


builder.Services.AddMudServices();
builder.Services.AddTransient<JwtDelegationHandler>();
builder.Services.AddBlazoredSessionStorage();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
