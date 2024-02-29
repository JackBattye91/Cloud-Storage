using Microsoft.IdentityModel.Tokens;

namespace CloudStorage.API
{
    public class CustomLifetimeValidator
    {
        public static bool Create(DateTime? dateFrom, DateTime? dateTo, SecurityToken token, TokenValidationParameters parameters)
        {
            DateTime now = DateTime.UtcNow;

            if (dateFrom != null && dateFrom >= now)
            {
                return false;
            }

            if (dateFrom != null && dateTo < now)
            {
                return false;
            }

            return true;
        }
    }
}
