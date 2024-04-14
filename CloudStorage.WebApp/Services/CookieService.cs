using Microsoft.JSInterop;

namespace CloudStorage.WebApp.Services
{
    public class CookieService
    {
        private readonly IJSObjectReference _jsObjectReference;
        public CookieService(IJSObjectReference jsObjectReference)
        {
            _jsObjectReference = jsObjectReference;
        }

        public async Task<IDictionary<string, string>> GetValues()
        {
            IDictionary<string, string> values = new Dictionary<string, string>();
            string? cookieValues = await _jsObjectReference.InvokeAsync<string?>("getCookies");

            if (!string.IsNullOrEmpty(cookieValues))
            {
                string[] cookieParts = cookieValues.Split(';');
                foreach (string cookiePart in cookieParts)
                {
                    string[] nameValuePair = cookiePart.Split('=');
                    if (nameValuePair.Length == 2)
                    {
                        values.Add(nameValuePair[0], nameValuePair[1]);
                    }
                }
            }

            return values;
        }


        public async Task<string?> GetValue(string key)
        {
            return await _jsObjectReference.InvokeAsync<string?>("getCookie", key);
        }
        public async Task<string?> SetValue(string key, string value)
        {
            return await _jsObjectReference.InvokeAsync<string?>("setCookie", key, value);
        }
    }
}
