using System.Text.RegularExpressions;

namespace CloudStorage.API.V2
{
    public class SecurityUtilities
    {
        public static bool ValidatePassword(string pPassword)
        {
            return Regex.Match(pPassword, Consts.RegEx.PASSWORD_VALIDATION).Success;
        }
    }
}
