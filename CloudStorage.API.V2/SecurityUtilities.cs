using System.Text.RegularExpressions;

namespace CloudStorage.API.V2
{
    public class SecurityUtilities
    {
        public static bool ValidatePassword(string? pPassword)
        {
            if (string.IsNullOrEmpty(pPassword))
            {
                return false;
            }

            if (pPassword.Length < 12)
            {
                return false;
            }

            return Regex.Match(pPassword, Consts.RegEx.PASSWORD_UPPERCASE_VALIDATION).Success &&
                Regex.Match(pPassword, Consts.RegEx.PASSWORD_LOWERCASE_VALIDATION).Success &&
                Regex.Match(pPassword, Consts.RegEx.PASSWORD_NUMBER_VALIDATION).Success &&
                Regex.Match(pPassword, Consts.RegEx.PASSWORD_SYMBOL_VALIDATION).Success;
        }
    }
}
