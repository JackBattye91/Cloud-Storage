using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace CloudStorage.WebApp.Services
{
    public class LocalStorageService
    {
        private readonly IJSObjectReference _jsObjectReference;
        public LocalStorageService(IJSObjectReference jsObjectReference)
        {
            _jsObjectReference = jsObjectReference;
        }

        public async Task<string?> GetValue(string key)
        {
            return await _jsObjectReference.InvokeAsync<string?>("localStorage.getItem", key);
        }
        public async Task<string?> SetValue(string key, string value)
        {
            return await _jsObjectReference.InvokeAsync<string?>("localStorage.setItem", key, value);
        }

        public async Task SetValue<T>(string key, T? obj)
        {
            string value = JsonConvert.SerializeObject(obj);
            await SetValue($"{key}", value ?? "");
        }
        public async Task<T?> GetValue<T>(string key)
        {
            string? objString = await GetValue($"{key}");
            return JsonConvert.DeserializeObject<T>(objString ?? "");
        }
    }
}
